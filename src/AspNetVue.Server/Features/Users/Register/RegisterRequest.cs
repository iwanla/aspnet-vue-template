namespace AspNetVue.Server.Features.Users.Register;

public sealed record RegisterRequest(string Email, string FirstName, string LastName, string Password);
