using System;
using System.Collections.Generic;

namespace Core.Cate.Models
{
    public class SysUserGuideConfigModel
    {
        public int GuideConfigID { get; set; }
        public string ScreenCode { get; set; }
        public int StepOrder { get; set; }
        public string Selector { get; set; }
        public string Title { get; set; }
        public string GuideText { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SysUserGuideScreenGroupModel
    {
        public string ScreenCode { get; set; }
        public string ScreenTitle { get; set; }
        public string IconClass { get; set; }
        public List<SysUserGuideConfigModel> Steps { get; set; } = new List<SysUserGuideConfigModel>();
    }

    public class SysUserGuideSaveModel
    {
        public string ScreenCode { get; set; }
        public List<SysUserGuideConfigModel> Steps { get; set; } = new List<SysUserGuideConfigModel>();
    }
}

