using System.IO;
using Newtonsoft.Json.Linq;

namespace tfstate_manipulator.Data
{
    public class StateReader
    {
        public JObject GetStateData(string stateFile)
        {
            string json = File.ReadAllText(stateFile);
            JObject state = JObject.Parse(json);
            return state;
        }
    }
}