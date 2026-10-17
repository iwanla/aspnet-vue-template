namespace AspNetVue.Server.Features.Identity.Register;

public sealed record RegisterRequest(string Email, string FirstName, string LastName, string Password);
