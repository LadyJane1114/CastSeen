namespace CastSeen.ViewModels
{
    internal enum DetailsTargetType
    {
        Actor,
        Movie
    }

    internal sealed class DetailsNavigationRequest
    {
        public DetailsNavigationRequest(DetailsTargetType targetType, string id)
        {
            TargetType = targetType;
            Id = id;
        }

        public DetailsTargetType TargetType { get; }
        public string Id { get; }
    }
}
