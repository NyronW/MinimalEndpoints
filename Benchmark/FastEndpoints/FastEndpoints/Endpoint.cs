using FastEndpoints;
using FluentValidation;

namespace FastEndpointsBench;


public class Request
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int Age { get; set; }
    public Address? Address { get; set; }
    public IEnumerable<string>? PhoneNumbers { get; set; }
    public string? Email { get; set; }
}

public class Address
{
    public string? Street { get; set; }
    public string? Apartment { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
}

public class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("firstname is required");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("lastname is required");
        RuleFor(x => x.Age).GreaterThan(18).WithMessage("Adults only");
        RuleFor(x => x.PhoneNumbers).NotEmpty().WithMessage("You must submit atleast a phone number");
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required");

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        RuleFor(x => x.Address).NotNull().SetValidator(new AddressValidator());
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
    }
}

public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street).NotEmpty().WithMessage("Street address is requied");
        RuleFor(x => x.City).NotEmpty().WithMessage("Address city address is requied");
        RuleFor(x => x.State).NotEmpty().WithMessage("Address state is requied");
        RuleFor(x => x.Country).NotEmpty().WithMessage("Address country is requied");
    }
}

public class Response
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Age { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

public class Endpoint : Endpoint<Request>
{
    public override void Configure()
    {
        Verbs(Http.POST);
        Routes("/benchmark/ok/{id}");
        AllowAnonymous();
    }

    public override Task HandleAsync(Request request, CancellationToken ct)
    {
        return SendAsync(new Response()
        {
            Id = request.Id,
            Name = request.FirstName + " " + request.LastName,
            Age = request.Age,
            PhoneNumber = request.PhoneNumbers?.FirstOrDefault(),
            Email = request.Email
        });
    }
}
