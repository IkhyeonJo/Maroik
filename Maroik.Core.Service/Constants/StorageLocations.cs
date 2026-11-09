using Maroik.Core.Domain.Uploads;

namespace Maroik.Core.Service.Constants;

/// <summary>The Domain's storage locations the Website serves, exposed by the Service layer, since the Website may not read the Domain.</summary>
public static class StorageLocations
{
    /// <summary>The URL prefix of an avatar: <see cref="AttachmentStoragePaths.AvatarDirectory"/> as a site path.</summary>
    public const string AvatarUrlPrefix = "/" + AttachmentStoragePaths.AvatarDirectory + "/";
}
