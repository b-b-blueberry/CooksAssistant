using StardewValley;

namespace LoveOfCooking.Interface
{
    public interface IManaBarAPI
    {
        int GetMana(Farmer farmer);
        void AddMana(Farmer farmer, int amt);

        int GetMaxMana(Farmer farmer);
        void SetMaxMana(Farmer farmer, int newMaxMana);
    }
}
