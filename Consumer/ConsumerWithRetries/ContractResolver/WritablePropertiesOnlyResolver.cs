using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ConsumerWithRetries.ContractResolver;

internal class WritablePropertiesOnlyResolver : DefaultContractResolver
{
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var props = base.CreateProperties(type, memberSerialization);
        return props.Where(p => p.Writable).ToList();
    }
}