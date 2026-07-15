using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PolicyPlatform.Application.Customers;
using PolicyPlatform.Application.Policies;
using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.McpServer;

/// <summary>MCP tools exposed to an agent for generating and managing insurance policies.
/// Thin wrappers over PolicyService/CustomerService — no business logic here, same rule
/// as the Api controllers: this layer only translates MCP <-> Application.</summary>
[McpServerToolType]
public static class PolicyTools
{
    [McpServerTool, Description("Creates a new customer record. Returns the customer id needed to create a policy.")]
    public static async Task<string> CreateCustomer(
        CustomerService customerService,
        [Description("Full name of the customer")] string fullName,
        [Description("Email address of the customer")] string email)
    {
        try
        {
            var customer = await customerService.CreateCustomerAsync(new CreateCustomerRequest(fullName, email));
            return JsonSerializer.Serialize(customer);
        }
        catch (DomainException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description(
        "Generates a new draft insurance policy for a customer with one or more coverages " +
        "(OC, AC, NNW). OC is mandatory before the policy can be activated. Coverage types " +
        "and sums are provided as a JSON array, e.g. " +
        "[{\"type\":\"OC\",\"sumInsured\":50000,\"premium\":800},{\"type\":\"AC\",\"sumInsured\":80000,\"premium\":1200}].")]
    public static async Task<string> GeneratePolicy(
        PolicyService policyService,
        [Description("Id of an existing customer (see create_customer)")] Guid customerId,
        [Description("Policy effective date, format yyyy-MM-dd")] string effectiveDate,
        [Description("Policy expiry date, format yyyy-MM-dd")] string expiryDate,
        [Description("JSON array of coverages: type (OC/AC/NNW), sumInsured, premium, optional currency (default PLN)")]
        string coveragesJson)
    {
        try
        {
            var coverages = JsonSerializer.Deserialize<List<CoverageRequest>>(
                coveragesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new DomainException("coveragesJson could not be parsed.");

            var request = new CreatePolicyRequest(
                customerId,
                DateOnly.Parse(effectiveDate),
                DateOnly.Parse(expiryDate),
                coverages);

            var policy = await policyService.CreatePolicyAsync(request);
            return JsonSerializer.Serialize(policy);
        }
        catch (DomainException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("Activates a draft policy. Fails if OC coverage is missing or the policy is not a draft.")]
    public static async Task<string> ActivatePolicy(PolicyService policyService, Guid policyId)
    {
        try
        {
            return JsonSerializer.Serialize(await policyService.ActivatePolicyAsync(policyId));
        }
        catch (DomainException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("Fetches a single policy by id, including its coverages and total premium.")]
    public static async Task<string> GetPolicy(PolicyService policyService, Guid policyId)
    {
        var policy = await policyService.GetPolicyAsync(policyId);
        return policy is null
            ? JsonSerializer.Serialize(new { error = $"Policy {policyId} was not found." })
            : JsonSerializer.Serialize(policy);
    }

    [McpServerTool, Description("Lists all policies currently known to the system.")]
    public static async Task<string> ListPolicies(PolicyService policyService)
        => JsonSerializer.Serialize(await policyService.ListPoliciesAsync());
}
