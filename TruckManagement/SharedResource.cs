using System.Reflection;
using Microsoft.Extensions.Localization;

public class SharedResource {}

public interface IResourceLocalizer 
{
    string Localize(string key);
}

public class ResourceLocalizer : IResourceLocalizer
{
    private readonly IStringLocalizer _localizer;
    
    public ResourceLocalizer(IStringLocalizerFactory factory)
    {
        var type = typeof(SharedResource);
        var assemblyName = new AssemblyName(type.Assembly.FullName!);
        _localizer = factory.Create("SharedResource", assemblyName.Name!);
    }

    public string Localize(string key)
    {
        return _localizer[key];
    }
}
