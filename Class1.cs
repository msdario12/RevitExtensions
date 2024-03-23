using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;


namespace RevitExtensions
{
  [Transaction(TransactionMode.Manual)]
  public class ModifiyColumns : IExternalCommand
  {
    public void Run(ExternalCommandData commandData, ref string message)
    {
      // Run application
      // Run application
      // Get the handle of current document.
      Document doc = commandData.Application.ActiveUIDocument.Document;
      Transaction transaction = new Transaction(doc, "Modify Columns");
      transaction.Start();
      try
      {
        FilteredElementCollector collector = new FilteredElementCollector(doc);
        ElementCategoryFilter filterRule = new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns);

        IList<Element> columns = collector.WherePasses(filterRule).WhereElementIsNotElementType().ToElements();
        // Call to get columns sorted
        IList<IList<Element>> sortedColumns = SortColumnsByLocation(columns, 0.1);
        // Number
        NumberColumns(sortedColumns);

        transaction.Commit();
      }
      catch (Exception e)
      {
        TaskDialog.Show("Error en Run", e.Message + e.StackTrace);
        message = e.Message;
        transaction.RollBack();
      }
    }

    private FilterCategoryRule ElementCategoryFilter(BuiltInCategory oST_StructuralColumns, FilteredElementCollector collector)
    {
      throw new NotImplementedException();
    }

    // Sort Columns
    private IList<IList<Element>> SortColumnsByLocation(IList<Element> columns, double tolerance)
    {
      IList<IList<Element>> groupedColumns = new List<IList<Element>>();

      foreach (Element column in columns)
      {
        var xPos = (column.Location as LocationPoint).Point.X;
        var yPos = (column.Location as LocationPoint).Point.Y;

        bool foundGroup = false;

        // Buscar si la columna pertenece a algún grupo existente
        foreach (var group in groupedColumns)
        {
          var referencedElement = group.FirstOrDefault();

          if (referencedElement != null)
          {
            var xRefPos = (referencedElement.Location as LocationPoint).Point.X;
            var yRefPos = (referencedElement.Location as LocationPoint).Point.Y;

            if (Math.Abs(yPos - yRefPos) <= tolerance)
            {
              group.Add(column);
              foundGroup = true;
              break;
            }
          }
        }

        // Si no se encontró un grupo existente, crear un nuevo grupo
        if (!foundGroup)
        {
          groupedColumns.Add(new List<Element> { column });
        }
      }

      return groupedColumns;
    }


    // Number Columns
    private void NumberColumns(IList<IList<Element>> columns)
    {
      int counter = 1;
      string columnNumber = "CTS";

      foreach (var group in columns.OrderBy(c => (c.First().Location as LocationPoint).Point.Y).ToList())
      {
        foreach (var element in group.OrderBy(c => (c.Location as LocationPoint).Point.X).ToList())
        {
          Parameter comment = element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
          comment.Set(columnNumber + counter);
          counter++;
        }
      }
    }



    // Run by default
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      try
      {
        ModifiyColumns instance = new ModifiyColumns();
        TaskDialog.Show("Prueba NET.4.8", "Esto es solo un aviso que inicio el programa");
        instance.Run(commandData, ref message);
      }
      catch (Exception e)
      {
        message = e.Message;
        TaskDialog.Show("Error en Execute", e.Message);
        return Autodesk.Revit.UI.Result.Failed;
      }

      return Autodesk.Revit.UI.Result.Succeeded;
    }
  }
}
