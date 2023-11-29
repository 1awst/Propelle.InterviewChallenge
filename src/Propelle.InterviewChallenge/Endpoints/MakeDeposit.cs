using FastEndpoints;
using Propelle.InterviewChallenge.Application;
using Propelle.InterviewChallenge.Application.Domain;
using Propelle.InterviewChallenge.Application.Domain.Events;

namespace Propelle.InterviewChallenge.Endpoints
{
    public static class MakeDeposit
    {
        public class Request
        {
            public Guid UserId { get; set; }

            public decimal Amount { get; set; }
        }

        public class Response
        {
            public Guid DepositId { get; set; }
        }

        public class Endpoint : Endpoint<Request, Response>
        {
            private readonly PaymentsContext _paymentsContext;
            private readonly Application.EventBus.IEventBus _eventBus;

            public Endpoint(
                PaymentsContext paymentsContext,
                Application.EventBus.IEventBus eventBus)
            {
                _paymentsContext = paymentsContext;
                _eventBus = eventBus;
            }

            public override void Configure()
            {
                Post("/api/deposits/{UserId}");
            }

            public override async Task HandleAsync(Request req, CancellationToken ct)
            {
                var deposit = new Deposit(req.UserId, req.Amount);
                _paymentsContext.Deposits.Add(deposit);

                await _paymentsContext.SaveChangesAsync(ct);

                while (true)
                {
                    try
                    {
                        await _eventBus.Publish(new DepositMade
                        {
                            Id = deposit.Id
                        });
                        break;
                    }
                    catch
                    {
                        // Retry until successful

                        // Note: this is a simple solution for the purposes of this interview test, however it's
                        // unsuitable for a production environment/real world scenario since it blocks until the event
                        // has been successfully published.
                    }
                }

                await SendAsync(new Response { DepositId = deposit.Id }, 201, ct);
            }
        }
    }
}