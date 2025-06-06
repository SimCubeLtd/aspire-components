namespace SimCube.Aspire.Components.Shared;

public static class StringExtensions
{
    public static string GetFinalForm(this string containerName, string containerNamePrefix)
    {
        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name cannot be null or whitespace.", nameof(containerName));
        }

        if (!string.IsNullOrEmpty(containerNamePrefix) && containerNamePrefix.Length > 0)
        {
            containerName = $"{containerNamePrefix}-{containerName}";
        }

        var namePrefixEnv = EnvironmentExtensions.GetNamePrefix();

        if (!string.IsNullOrEmpty(namePrefixEnv))
        {
            containerName = $"{namePrefixEnv}-{containerName}";
        }

        return containerName;
    }
}
