namespace MacondoTech.EnterpriseBus.Contracts.Responses
{
    public interface SubmitOrderResponse
    {
        string OrderId { get; }
        string OrderDescription { get; }
        Datetime CreationTime { get; set; }
    }

}