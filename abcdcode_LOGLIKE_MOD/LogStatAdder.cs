// -----------------------------------------------------------------------------
// Library of Ruina mod script: LogStatAdder
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\LogStatAdder.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using GameSave;
using System;
using System.Reflection;


namespace abcdcode_LOGLIKE_MOD
{

    /// <summary>LOGLIKE type: LogStatAdder</summary>

    public class LogStatAdder : Savable
    {
        public int maxhp;
        public int maxhppercent;
        public int maxbreak;
        public int maxbreakpercent;
        public int rangetype = -1;
        public int maxplaypoint;
        public int startplaypoint;
        public int speeddicenum;
        public int speedmin;
        public int speedmax;
       
        public virtual SaveData GetSaveData()
        {
            SaveData saveData = new SaveData();
            saveData.AddData("maxhp", new SaveData(this.maxhp));
            saveData.AddData("maxhppercent", new SaveData(this.maxhppercent));
            saveData.AddData("maxbreak", new SaveData(this.maxbreak));
            saveData.AddData("maxbreakpercent", new SaveData(this.maxbreakpercent));
            saveData.AddData("rangetype", new SaveData(this.rangetype));
            saveData.AddData("maxplaypoint", maxplaypoint);
            saveData.AddData("startplaypoint", startplaypoint);
            saveData.AddData("speeddicenum", speeddicenum);
            saveData.AddData("speedmin", speedmin);
            saveData.AddData("speedmax", speedmax);

            saveData.AddData("TypeName", new SaveData(this.GetType().Name));
            return saveData;
        }

        public static LogStatAdder CreateStatAdderBySave(SaveData save)
        {
            string str = save.GetString("TypeName");
            foreach (Assembly assem in LogLikeMod.GetAssemList())
            {
                foreach (System.Type type in LogLikeMod.GetLoadableTypes(assem))
                {
                    if (type.Name == str)
                        return Activator.CreateInstance(type) as LogStatAdder;
                }
            }
            return (LogStatAdder)null;
        }

        public virtual void LoadFromSaveData(SaveData data)
        {
            this.maxhp = data.GetData("maxhp").GetIntSelf();
            this.maxhppercent = data.GetData("maxhppercent").GetIntSelf();
            this.maxbreak = data.GetData("maxbreak").GetIntSelf();
            this.maxbreakpercent = data.GetData("maxbreakpercent").GetIntSelf();
            this.rangetype = data.GetData("rangetype").GetIntSelf();
            this.maxplaypoint = data.GetInt("maxplaypoint");
            this.startplaypoint = data.GetInt("startplaypoint");
            this.speeddicenum = data.GetInt("speeddicenum");
            this.speedmax = data.GetInt("speedmax"); 
            this.speedmin = data.GetInt("speedmin");
        }

        public virtual AtkResist GetResist(AtkResistType type, AtkResist baseResist) => baseResist;

        public virtual EquipRangeType GetRangeType(EquipRangeType cur)
        {
            return this.rangetype == -1 ? cur : (EquipRangeType)this.rangetype;
        }
    }
}
