
/// <summary>
/// Defines a contract for components that need to react to changes in language settings.
/// Components implementing this interface can subscribe to language changes and update their content dynamically,
/// ensuring the application can support multilingual functionality seamlessly.
/// </summary>
public interface ILocalizable
{
    void OnLocalizationChanged();
}