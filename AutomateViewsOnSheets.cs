using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace RevitExtensions
{
    public class WindowView
    {
        public View Plan { get; set; }
        public View Elevation { get; set; }
        public View Cross { get; set; }
    }
    internal class AutomateViewsOnSheets
    {

        public static void CreateWindowSheets(Document doc)
        {
            // Global
            ElementId defaultTitleBlockId = doc.GetDefaultFamilyTypeId(new ElementId(BuiltInCategory.OST_TitleBlocks));

            // Get All Views + Filter
            var allViews = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Views)
                .WhereElementIsNotElementType()
                .Cast<View>();

            var viewsToPlace = allViews.Where(view => view.Name.Contains("API_"));

            // Desired Dict Structure
            // dictViews = {'VIEW_NAME' : {'Plan': null, 'Elevation': null, 'Cross': null}}

            // Sort Views for Placing on Sheets
            var dictViews = new Dictionary<string, WindowView>();

            // Sort Views to Place
            foreach (var view in viewsToPlace)
            {
                try
                {
                    string viewName = view.Name.Replace("py_", ""); // Remove py_ prefix
                    //string winName = viewName.Split('(')[0].Trim();

                    //if (!dictViews.ContainsKey(winName))
                    //    dictViews[winName] = new WindowView();

                    //if (view.Name.Contains("(Plan)"))
                    //    dictViews[winName].Plan = view;
                    //else if (view.Name.Contains("(Cross)"))
                    //    dictViews[winName].Cross = view;
                    //else if (view.Name.Contains("(Elevation)"))
                    //    dictViews[winName].Elevation = view;

                    var newView = new WindowView();
                    newView.Cross = view;
                    dictViews.Add(viewName, newView);
                }
                catch
                {
                    continue;
                }
            }

            {

                // Iterate and Create New Sheet
                // Transaction to Make changes
                using (Transaction trans = new Transaction(doc, "Create Window Sheets"))
                {
                    trans.Start();
                    foreach (KeyValuePair<string, WindowView> kvp in dictViews)
                    {
                        string winName = kvp.Key;
                        WindowView windowViews = kvp.Value;

                        // Get Plan/Cross/Elevation Views
                        View plan = windowViews.Plan;
                        View elev = windowViews.Elevation;
                        View cros = windowViews.Cross;

                        // Handle Errors during view placement (SubTransaction)
                        using (SubTransaction st = new SubTransaction(doc))
                        {
                            st.Start();

                            // Create new ViewSheet
                            ViewSheet newSheet = ViewSheet.Create(doc, defaultTitleBlockId);

                            // Check if possible to place views
                            if (cros != null && Viewport.CanAddViewToSheet(doc, newSheet.Id, cros.Id))
                            {
                                st.Commit();

                                // Define position for placing views
                                var ptPlan = new XYZ(-0.4, 0.3, 0);
                                var ptCros = new XYZ(0, 0, 0);
                                var ptElev = new XYZ(-0.4, 0.75, 0);

                                // Place Views on Sheets
                                //Viewport.Create(doc, newSheet.Id, plan.Id, ptPlan);
                                Viewport.Create(doc, newSheet.Id, cros.Id, ptCros);
                                //Viewport.Create(doc, newSheet.Id, elev.Id, ptElev);

                                // Rename Sheets
                                try
                                {
                                    newSheet.SheetNumber = $"Window-{winName}";
                                    newSheet.Name = "";
                                }
                                catch { }
                            }
                            else
                            {
                                TaskDialog.Show("Error", $"The following window sections already placed or missing views: {winName}");
                                continue;
                            }
                        }
                    }
                    trans.Commit();
                }
            }
        }

    }
}
