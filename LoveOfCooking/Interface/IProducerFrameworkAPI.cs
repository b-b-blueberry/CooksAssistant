namespace LoveOfCooking.Interface
{
	public interface IProducerFrameworkAPI
    {
        /// <summary>
        /// Adds a content pack from the specified directory.
        /// This method expects a content-pack.json file instead of a manifest.json
        /// </summary>
        /// <param name="directory">The absolute path of the content pack.</param>
        /// <returns>true if the content pack was successfully loaded, otherwise false.</returns>
        bool AddContentPack(string directory);
    }
}
