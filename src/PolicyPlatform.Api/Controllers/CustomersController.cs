using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Customers;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly CustomerService _customerService;

    public CustomersController(CustomerService customerService) => _customerService = customerService;

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _customerService.CreateCustomerAsync(request, ct));
        }
        catch (DomainException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
