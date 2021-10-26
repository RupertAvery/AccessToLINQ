using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace AccessToLinq
{
    public static class ProxyTypeBuilder
    {
        private static readonly ModuleBuilder ModuleBuilder;
        private static Dictionary<string, Type> typeCache;

        static ProxyTypeBuilder()
        {
            var an = new AssemblyName("AccessProxyAssembly");
            typeCache = new Dictionary<string, Type>();
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            ModuleBuilder = assemblyBuilder.DefineDynamicModule("AccessProxyModule");
        }

        public static Type GetOrCreateProxyType(Type sourceType)
        {
            var typeSignature = sourceType.Name + "_AccessProxy";

            if (typeCache.TryGetValue(typeSignature, out Type type))
            {
                return type;
            }

            var tb = GetTypeBuilder(sourceType, typeSignature);
            tb.AddInterfaceImplementation(typeof(IProxy));

            var tr = tb.DefineField("IsTrackingEnabled", typeof(bool), FieldAttributes.Family);
            var fb = tb.DefineField("IsDirty", typeof(Dictionary<string, bool>), FieldAttributes.Family);

            var et = tb.DefineMethod("EnableTracking", MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.HasThis, null, Type.EmptyTypes);
            ILGenerator getIl3 = et.GetILGenerator();
            getIl3.Emit(OpCodes.Nop);
            getIl3.Emit(OpCodes.Ldarg_0);
            getIl3.Emit(OpCodes.Ldc_I4_1);
            getIl3.Emit(OpCodes.Stfld, tr);
            getIl3.Emit(OpCodes.Ret);


            var ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            ILGenerator getIlc = ctor.GetILGenerator();
            getIlc.Emit(OpCodes.Ldarg_0);
            getIlc.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            getIlc.Emit(OpCodes.Nop);
            getIlc.Emit(OpCodes.Nop);
            getIlc.Emit(OpCodes.Ldarg_0);
            getIlc.Emit(OpCodes.Newobj, typeof(Dictionary<string, bool>).GetConstructor(Type.EmptyTypes));
            getIlc.Emit(OpCodes.Stfld, fb);
            getIlc.Emit(OpCodes.Ret);

            var mb = tb.DefineMethod("SetDirty", MethodAttributes.Private | MethodAttributes.HideBySig, CallingConventions.HasThis, null, new[] { typeof(string) });
            ILGenerator getIl = mb.GetILGenerator();
            Label l = getIl.DefineLabel();
            var setItemMethod = typeof(Dictionary<string, bool>).GetMethod("set_Item");
            getIl.Emit(OpCodes.Nop);
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, tr);
            getIl.Emit(OpCodes.Brfalse_S, l);
            getIl.Emit(OpCodes.Nop);
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fb);
            getIl.Emit(OpCodes.Ldarg_1);
            getIl.Emit(OpCodes.Ldc_I4_1);
            getIl.Emit(OpCodes.Callvirt, setItemMethod);
            getIl.Emit(OpCodes.Nop);
            getIl.Emit(OpCodes.Nop);
            getIl.MarkLabel(l);
            getIl.Emit(OpCodes.Ret);

            var mb2 = tb.DefineMethod("GetDirtyProperties", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual, CallingConventions.HasThis, typeof(Dictionary<string, bool>), Type.EmptyTypes);
            ILGenerator getIl2 = mb2.GetILGenerator();
            getIl2.Emit(OpCodes.Nop);
            getIl2.Emit(OpCodes.Ldarg_0);
            getIl2.Emit(OpCodes.Ldfld, fb);
            getIl2.Emit(OpCodes.Ret);

            foreach (var property in sourceType.GetProperties())
            {
                CreateProperty(tb, property.Name, property.PropertyType, property, mb);
            }

            type = tb.CreateType();

            typeCache.Add(typeSignature, type);

            return type;
        }

        private static TypeBuilder GetTypeBuilder(Type parent, string typeSignature)
        {
            return ModuleBuilder.DefineType(typeSignature,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.AutoLayout,
                parent);
        }

        private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType, PropertyInfo property, MethodInfo setDirtyMethod)
        {
            FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            tb.DefineMethodOverride(getPropMthdBldr, property.GetMethod);

            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                    MethodAttributes.Public |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig | MethodAttributes.Virtual,
                    null, new[] { propertyType });

            tb.DefineMethodOverride(setPropMthdBldr, property.SetMethod);

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();

            setIl.Emit(OpCodes.Nop);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldstr, propertyName);
            setIl.Emit(OpCodes.Callvirt, setDirtyMethod);

            setIl.Emit(OpCodes.Nop);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
    }
}