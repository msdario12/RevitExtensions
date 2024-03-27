using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace RevitExtensions
{
    internal class App : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            RibbonPanel panel = RibbonPanel(application);
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            if (panel.AddItem(new PushButtonData("CreateSectionsView", "CreateSectionsView", thisAssemblyPath, "RevitExtensions.CreateSectionsView"))
                is PushButton button)
            {
                button.ToolTip = "CreateSectionViews";

                Uri uri = new Uri(Path.Combine(Path.GetDirectoryName(thisAssemblyPath), "Resources", "icon.ico"));
                BitmapImage bitmapImage = new BitmapImage(uri);
                button.LargeImage = bitmapImage;

            }

            return Result.Succeeded;

        }
        public RibbonPanel RibbonPanel(UIControlledApplication a)
        {
            string tab = "DarioDev";

            RibbonPanel ribbonPanel = null;

            try
            {
                a.CreateRibbonTab(tab);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            try
            {
                RibbonPanel panel = a.CreateRibbonPanel(tab, "Dario");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            List<RibbonPanel> panels = a.GetRibbonPanels(tab);
            foreach (RibbonPanel p in panels.Where(p => p.Name == "Dario"))
            {
                ribbonPanel = p;
            }

            return ribbonPanel;


        }


    }
}
