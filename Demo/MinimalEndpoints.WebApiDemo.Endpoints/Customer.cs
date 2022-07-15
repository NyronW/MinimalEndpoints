namespace MinimalEndpoints.WebApiDemo.Endpoints;
/// <summary>
/// A customer object
/// </summary>
/// <param name="Id">Unique customer identifier</param>
/// <param name="Name">Customer fullname</param>
public record Customer(int Id, string Name);