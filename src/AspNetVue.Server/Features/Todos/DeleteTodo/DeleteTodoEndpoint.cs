using AspNetVue.Server.Common;
using AspNetVue.Server.Common.Endpoints;
using AspNetVue.Server.Common.Extensions;
using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Todos;

public sealed class DeleteTodoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("todos/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteTodoCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteTodoCommand(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Todos)
        .RequireAuthorization();
    }
}
