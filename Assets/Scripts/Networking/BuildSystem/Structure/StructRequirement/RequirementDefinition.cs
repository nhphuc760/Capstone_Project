using System;
using Fusion;

[Serializable]
public abstract class RequirementDefinition
{
    public abstract IRequirement CreateRuntime(RequirementServices services, NetworkObject source = default);
}
