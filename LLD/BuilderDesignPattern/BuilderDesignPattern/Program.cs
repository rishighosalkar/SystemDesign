using BuilderDesignPattern;

var query = new SQLQueryBuilder()
    .Select("name", "email", "age")
    .From("users")
    .Where("age > 18")
    .Where("active = 1")
    .OrderBy("name")
    //.Limit(10)
    .Build();

Console.WriteLine(query);
// Output: SELECT name, email, age FROM users WHERE age > 18 AND active = 1 ORDER BY name ASC LIMIT 10
