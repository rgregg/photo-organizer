using System;
using System.Collections.Generic;
using System.Text;

namespace MediaOrganizerConsoleApp
{
    interface IProgressReporter
    {
        void ShowProgressBar(int maxItems);
        void HideProgressBar();
        void ReportProgress(int currentProgress);
    }
}
