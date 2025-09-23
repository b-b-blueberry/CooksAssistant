using StardewValley.Objects;
using System.Collections.Generic;

namespace LoveOfCooking.Interface
{
    public interface IRemoteFridgeAPI
    {
        IEnumerable<Chest> GetFridgeChests();
    }
}
