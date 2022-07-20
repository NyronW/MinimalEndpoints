using System.ComponentModel.DataAnnotations;

namespace MinimalEndpoints.WebApiDemo.Endpoints;
/// <summary>
/// A customer object
/// </summary>
/// <param name="Id">Unique customer identifier</param>
/// <param name="Name">Customer fullname</param>
public class Customer
{
    /// <summary>
    /// Xml Serialization needs a parameterless contructor
    /// </summary>
    public Customer()
    {

    }

    public Customer(int id, string name)
    {
        Id = id;
        Name = name;
    }

    [Required]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; }
}

/// <summary>
/// Customer Dto
/// </summary>
public class CustomerDto
{
    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }
}