namespace AccessToLinq
{
    class RawSQL
    {
        public string SQL { get; private set; }
        public object Reference { get; private set; }

        public RawSQL(object reference, string value)
        {
            this.Reference = reference;
            SQL = value;
        }

        public RawSQL(string value)
        {
            SQL = value;
        }

        public override string ToString()
        {
            return SQL;
        }

        public static RawSQL operator +(RawSQL rawql, string value)
        {
            return new RawSQL(rawql.ToString() + value);
        }
    }
}