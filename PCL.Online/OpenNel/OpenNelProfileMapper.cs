using System;

namespace PCL.Online.OpenNel;

public static class OpenNelProfileMapper
{
    public static OpenNelPortableProfile ToPortableProfile(OpenNelAccountResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new OpenNelPortableProfile(
            Uuid: result.EntityId,
            Username: result.DisplayName,
            AccessToken: result.AccessToken,
            LoginKind: result.LoginKind,
            DetailsJson: result.PersistedDetailsJson);
    }
}
