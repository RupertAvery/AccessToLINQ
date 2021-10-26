namespace AccessToLINQ.Expressions
{
    class JoinTable
    {
        public object obj1;
        //static Dictionary<object, string> joinRefCounter = new Dictionary<object, string>();
        readonly string _alias = "";

        public JoinTable(object obj1, ref int count)
        {
            if (obj1.GetType() != typeof(Join))
            {
                _alias = "t" + count.ToString();
                count++;
                //joinRefCounter.Add(this, tinf);

            }// TODO: Complete member initialization
            this.obj1 = obj1;
        }

        public string Alias => _alias;

        public string getField(string name)
        {
            if (obj1.GetType() != typeof(Join))
            {
                return _alias + "." + name;
            }
            else
            {
                return ((Join)obj1).getField(name);
            }
        }
    }
}