namespace BuilderDesignPattern
{
    public class SQLQueryBuilder
    {
        private string _tableName;
        private List<string> _columns = new List<string>();
        private List<string> _conditions = new List<string>();
        private List<string> _orderBy = new List<string>();
        public SQLQueryBuilder Select(params string[] columns)
        {
            _columns.AddRange(columns);
            return this;
        }

        public SQLQueryBuilder From(string tableName)
        {
            _tableName = tableName;
            return this;
        }

        public SQLQueryBuilder Where(string condition)
        { 
            _conditions.Add(condition); 
            return this; 
        }

        public SQLQueryBuilder OrderBy(string orderBy)
        {
            _orderBy.Add(orderBy);
            return this;
        }

        public string Build()
        {
            string columns = string.Join(", ", _columns);

            string query = $"select {columns} from {_tableName}";
            
            if(_conditions.Count > 0)
            {
                query += $" WHERE {string.Join(" AND ", _conditions)}";
            }

            if(_orderBy.Count > 0)
            {
                query += $" ORDER BY {string.Join(", ", _orderBy)}";
            }

            return query;
        }
    }
}
