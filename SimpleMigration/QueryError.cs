namespace SimpleMigration
{
    public class QueryError
    {
        public string File { get; set; }

        public int StartLine { get; set; }

        public string Query { get; set; }
    }
}