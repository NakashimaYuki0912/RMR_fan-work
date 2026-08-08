// -----------------------------------------------------------------------------
// Library of Ruina mod script: StagesXmlList
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\StagesXmlList.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;


namespace abcdcode_LOGLIKE_MOD
{

    /// <summary>StagesXmlList</summary>

    public class StagesXmlList : Singleton<StagesXmlList>
    {
        public List<StagesXmlInfo> infos;
        private StagesXmlInfo[] _allInfos;
        public void Init(List<StagesXmlInfo> info)
        {
            this.infos = info;
            foreach (StagesXmlInfo stagesXmlInfo in info)
            {
                foreach (LogueStageInfo stage in stagesXmlInfo.Stages)
                    LogLikeMod.RegisterPickUpXml(stage);
            }
            this._allInfos = new StagesXmlInfo[info.Count];
            info.CopyTo(this._allInfos);
        }

        public List<LogueStageInfo> GetChapterData(ChapterGrade chapter, bool ExceptAllGrade = false)
        {
            List<LogueStageInfo> chapterData = new List<LogueStageInfo>();
            foreach (StagesXmlInfo stagesXmlInfo in this.infos.FindAll(x =>
            {
                if (x.chapter == chapter)
                    return true;
                return !ExceptAllGrade && x.chapter == ChapterGrade.GradeAll;
            }))
                chapterData.AddRange(stagesXmlInfo.Stages);
            return chapterData;
        }

        public List<LogueStageInfo> GetChapterDataVanilla(ChapterGrade chapter, bool ExceptAllGrade = false)
        {
            return GetChapterDataSpecific(chapter, new LorId(LogLikeMod.ModId, 0), ExceptAllGrade);
        }

        /// <summary>
        /// Returns a list containing every single reception for a given chapter for a specific campaign.
        /// </summary>
        /// <param name="chapter">The chapter to filter by.</param>
        /// <param name="id">The campaign ID of the campaign.</param>
        /// <param name="ExceptAllGrade">Whether or not to exclude encounters that can show up in any chapter.</param>
        /// <returns></returns>
        public List<LogueStageInfo> GetChapterDataSpecific(ChapterGrade chapter, LorId id, bool ExceptAllGrade = false)
        {
            List<LogueStageInfo> chapterData = new List<LogueStageInfo>();
            foreach (StagesXmlInfo stagesXmlInfo in this.infos.FindAll(x =>
            {
                if (x.Id != id)
                    return false;
                if (x.chapter == chapter)
                    return true;
                return !ExceptAllGrade && x.chapter == ChapterGrade.GradeAll;
            }))
                chapterData.AddRange(stagesXmlInfo.Stages);
            return chapterData;
        }

        public LogueStageInfo GetStageInfo(LorId stageid)
        {
            return GetStageInfo(stageid, preferredType: null);
        }

        /// <summary>
        /// Resolve a stage template by id. When <paramref name="preferredType"/> is set,
        /// prefer an entry whose StageType matches (avoids Rest/Shop ID collisions).
        /// </summary>
        public LogueStageInfo GetStageInfo(LorId stageid, StageType? preferredType)
        {
            if (stageid == null || this.infos == null)
                return null;

            LogueStageInfo fallback = null;
            foreach (StagesXmlInfo info in this.infos)
            {
                if (info?.Stages == null)
                    continue;
                foreach (LogueStageInfo stage in info.Stages)
                {
                    if (stage == null || stage.Id != stageid)
                        continue;
                    if (preferredType.HasValue && stage.type == preferredType.Value)
                        return stage.Copy();
                    if (fallback == null)
                        fallback = stage;
                }
            }
            return fallback != null ? fallback.Copy() : null;
        }

        public void RestoreToDefault()
        {
            this.infos.Clear();
            this.infos.AddRange((IEnumerable<StagesXmlInfo>)((IEnumerable<StagesXmlInfo>)this._allInfos).ToList<StagesXmlInfo>());
        }
    }
}
