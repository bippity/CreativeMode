using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using TShockAPI;

namespace CreativeMode
{
    class Config
    {
        private static string savepath = TShock.SavePath;
        public static Contents contents;

        public class Contents
        {
            public bool EnableBlacklist = false;
            public List<int> BlacklistItems = new List<int>()
#region BlacklistItems           
            {
                    11, //Iron ore
                    12, //Copper ore
                    13, //Gold ore
                    14, //Silver ore
                    56, //Demonite ore
                    364, //Cobalt ore
                    365, //Mythril ore
                    366, //Adamantite ore
                    699, //Tin ore
                    700, //Lead ore
                    701, //Tungsten ore
                    702, //Platinum ore
                    880, //Crimtane ore
                    947, //Chlorophyte ore
                    1104, //Palladium ore
                    1105, //Orichalcum ore
                    1106 //Titanium ore
             };
#endregion
        }

        public static void CreateConfig()
        {
            string filepath = Path.Combine(savepath, "CreativeModeConfig.json");
            
            try
            {
                using (var stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    using (var sr = new StreamWriter(stream))
                    {
                        contents = new Contents();
                        var configString = JsonConvert.SerializeObject(contents, Formatting.Indented);
                        sr.Write(configString);
                    }
                    stream.Close();
                }
            }
            catch (Exception e)
            {
                Log.ConsoleError(e.Message);
                contents = new Contents();
            }
        }

        public static bool ReadConfig()
        {
            string filepath = Path.Combine(savepath, "CreativeModeConfig.json");

            try
            {
                if (File.Exists(filepath))
                {
                    using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            var configString = sr.ReadToEnd();
                            contents = JsonConvert.DeserializeObject<Contents>(configString);
                        }
                        stream.Close();
                    }
                    return true;
                }
                else
                {
                    CreateConfig();
                    Log.ConsoleInfo("Created CreativeModeConfig.json.");
                    return true;
                }
            }
            catch (Exception e)
            {
                Log.ConsoleError(e.Message);
            }
            return false;
        }

        public static bool UpdateConfig()
        {
            string filepath = Path.Combine(savepath, "CreativeModeConfig.json");

            try
            {
                if (!File.Exists(filepath))
                    return false;

                string query = JsonConvert.SerializeObject(contents, Formatting.Indented);
                using (var stream = new StreamWriter(filepath, false))
                {
                    stream.Write(query);
                }
                return true;
            }
            catch (Exception e)
            {
                Log.ConsoleError(e.Message);
                return false;
            }
        }
    }
}
