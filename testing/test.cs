using System;
using System.Collections;
using System.Diagnostics;
using Tekla.Structures.Drawing;
using Tekla.Structures.Geometry3d;
using T3D = Tekla.Structures.Geometry3d;
//using Tekla.Structures.Model;
using Tekla.Structures.Model.Operations;
using Tekla.Structures.Solid;    

namespace Tekla.Technology.Akit.UserScript                                                                                /////////
{                                                                                                                         /////////
    public static class Script                                                                                            /////////
    {                                                                                                                     /////////
        public static void Run(Tekla.Technology.Akit.IScript akit)                                                        /////////
        {                                                                                                                 /////////
            try                                                                                                           /////////
            {                                                                                                             /////////
                new Example1();                                                                                      /////////
            }                                                                                                             /////////
            catch (Exception)                                                                                             /////////
            { }                                                                                                           /////////
        }                                                                                                                 /////////
        static void Main(string[] args)                                                                                   /////////
        {                                                                                                                 /////////
            try                                                                                                           /////////
            {                                                                                                             /////////
                new Example1();                                                                                      /////////
            }                                                                                                             /////////
            catch (Exception)                                                                                             /////////
            { }                                                                                                           /////////
        }                                                                                                                 /////////
    }       
    class Example1
    {
        public Example1()
        {
            Drawing MyDrawing = new GADrawing();
            View curview = new View(MyDrawing.GetSheet(), new CoordinateSystem(), new CoordinateSystem(),
                new AABB(new Point(), new Point(30000, 30000, 10000)));
            Grid curGrid;

            DrawingObjectEnumerator allObjects = curview.GetAllObjects();
            while (allObjects.MoveNext())
            {
                if (allObjects.Current is Grid)
                {
                    curGrid = allObjects.Current as Grid;
                    curGrid.Attributes.DrawTextAtTopOfGrid = true;
                    curGrid.Attributes.Font.Color = DrawingColors.Red;
                    curGrid.Modify(); /* Apply changes */
                }
            }
        }
    }
   
}