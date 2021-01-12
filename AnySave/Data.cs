using System.Collections.Generic;

namespace Su226.AnySave {
  class CharacterData {
    public string map;
    public float x;
    public float y;
    public int facing;
  }
  class FarmerData {
    public string map;
    public float x;
    public float y;
    public int facing;
    public bool swimming;
    public bool swimSuit;
    public string horse;
  }
  class SaveData {
    public Dictionary<string, CharacterData> character;
    public Dictionary<long, FarmerData> farmer;
    public int time;
  }
}