namespace Su226.AnySave {
  class PathData {
    public string map;
    public int x;
    public int y;
    public int facing;
  }
  class CharacterData {
    public string map;
    public float x;
    public float y;
    public int facing;
    public PathData target;
    public int[] queued;
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
  class TimeData {
    public int time;
  }
}