using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using Tekla.Structures.Drawing;
using T3D = Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using Tekla.Technology.Akit.UserScript;
using Tekla.Structures;

namespace Tekla.Technology.Akit.UserScript
{
    public static class Script
    {
        public static void Run(Tekla.Technology.Akit.IScript akit)
        {
            try
            {
                new BoltDimension();
            }
            catch (Exception)
            { }
        }
        static void Main(string[] args)
        {
            try
            {
                new BoltDimension();
            }
            catch (Exception)
            { }
        }
    }
    
    class BoltDimension
    {
        private const string PickBoltGroup = "Pick bolt group to dimension.";
        private const string PickPart = "Pick part to dimension bolts in relation to.";
        private const string PickPoint = "Pick point to dimension bolts relative to.";

        public BoltDimension()
        {
            var dh1 = new DrawingHandler();

            //Check if drawing is open
            var openDrawing = dh1.GetActiveDrawing();
            if (openDrawing == null)
            {
                MessageBox.Show("You must have a drawing open first to use this tool.");
                return;
            }

            //View variable to pass to picker
            Tekla.Structures.Drawing.ViewBase viewBase1;
            Tekla.Structures.Drawing.ViewBase viewBase2;
            Tekla.Structures.Drawing.ViewBase viewBase3;
            Tekla.Structures.Drawing.DrawingObject pickedObject1;
            Tekla.Structures.Drawing.DrawingObject pickedObject2;
            Tekla.Structures.Geometry3d.Point pickedPoint;
            
            //Get model bolt from picked object
            var picker = dh1.GetPicker();
            picker.PickObject(PickBoltGroup, out pickedObject1, out viewBase1);
            if (pickedObject1 == null || viewBase1 == null || !(pickedObject1 is Tekla.Structures.Drawing.Bolt))
            {
                MessageBox.Show("No drawing bolt found for first picked object.");
                return;
            }
            var drawingBolt = pickedObject1 as Tekla.Structures.Drawing.Bolt;
            drawingBolt.Select();
            var modelBolt = new Model().SelectModelObject(drawingBolt.ModelIdentifier);

            //Get model part from picked object
            picker.PickObject(PickPart, out pickedObject2, out viewBase2);
            if (pickedObject2 == null || viewBase2 == null || !(pickedObject2 is Tekla.Structures.Drawing.Part))
            {
                MessageBox.Show("No drawing part found for 2nd picked object.");
                return;
            }
            var drawingPart = pickedObject2 as Tekla.Structures.Drawing.Part;
            var result = drawingPart.Select();
            Trace.WriteLine("Part select result: " + result);
            var modelPart = new Model().SelectModelObject(drawingPart.ModelIdentifier);

            //Get point from user
            picker.PickPoint(PickPoint, out pickedPoint, out viewBase3);
            if (pickedPoint == null || viewBase3 == null)
            {
                MessageBox.Show("Unable to get picked point from drawing.");
                return;
            }

            //Get view from picked objects
            var tView = pickedObject1.GetView() as Tekla.Structures.Drawing.View;
            if (tView == null)
            {
                MessageBox.Show("Unable to find view for object picked.");
                return;
            }
            tView.Select();

            //Verify model part and bolt are not null
            if (modelPart == null || modelBolt == null)
            {
                MessageBox.Show("Unable to get model object from drawing using Id's.");
                return;
            }

            //Call specific methods for beams vs. contour plates
            if (modelPart is Tekla.Structures.Model.Beam)
            {
                var tBeam = (Tekla.Structures.Model.Beam)modelPart;
                var tBoltGoup = (Tekla.Structures.Model.BoltGroup)modelBolt;
                CreateBoltDimensionToBeam(tView, tBeam, tBoltGoup, pickedPoint);
            }
            else if (modelPart is Tekla.Structures.Model.ContourPlate)
            {
                var tContourPlate = (Tekla.Structures.Model.ContourPlate)modelPart;
                var tBoltGoup = (Tekla.Structures.Model.BoltGroup)modelBolt;
                CreateBoltDimensionToPlate(tView, tContourPlate, tBoltGoup, pickedPoint);
            }
        }

        private static void CreateBoltDimensionToPlate(Tekla.Structures.Drawing.View tView, Tekla.Structures.Model.ContourPlate tPlate,
            Tekla.Structures.Model.BoltGroup tBoltGroup, Tekla.Structures.Geometry3d.Point tPickedPoint)
        {
            const double distancePast = 200.0;
            var originalPlane = new Model().GetWorkPlaneHandler().GetCurrentTransformationPlane();
            try
            {
                //Clear workplane to new blank global
                new Model().GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane());

                //Set model everything to the view
                new Model().GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane(tView.DisplayCoordinateSystem));
                tPlate.Select();
                tBoltGroup.Select();
                tView.Select();

                // This part modified for DS ----------------------------- //
                var dimensionPoints = new ArrayList();                     //
                                                                           //
                //Add picked point by user                                 //
                dimensionPoints.Add(tPickedPoint);                         //
                                                                           //
                //Add bolt positions                                       //
                dimensionPoints.AddRange(tBoltGroup.BoltPositions);        //
                // ------------------------------------------------------- //

                //var dimSetAttributes = new Tekla.Structures.Drawing.attributes("standard");
                var dimVector = new T3D.Vector(0, 1, 0);
                var perpVector = dimVector.Cross(new T3D.Vector(0, 0, 1));

                //Add dimensions
                var newDimSet = new Tekla.Structures.Drawing.StraightDimensionSetHandler();
                newDimSet.CreateDimensionSet(tView, CovertArrayListToPointList(dimensionPoints), dimVector, distancePast);
                newDimSet.CreateDimensionSet(tView, CovertArrayListToPointList(dimensionPoints), perpVector, distancePast);
               
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            finally
            {
                new Model().GetWorkPlaneHandler().SetCurrentTransformationPlane(originalPlane);
            }
        }

        private static void CreateBoltDimensionToBeam(Tekla.Structures.Drawing.View tView, Tekla.Structures.Model.Beam tBeam,
                        Tekla.Structures.Model.BoltGroup tBoltGroup, Tekla.Structures.Geometry3d.Point tPickedPoint)
        {
            if (tView == null || tBeam == null || tBoltGroup == null)
                throw new ArgumentNullException();

            const double distancePast = 200.0;
            var originalPlane = new Model().GetWorkPlaneHandler().GetCurrentTransformationPlane();
            try
            {
                //Clear workplane to new blank global
                new Model().GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane());

                //Set model everything to the view
                new Model().GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane(tView.DisplayCoordinateSystem));
                tBeam.Select();
                tBoltGroup.Select();

                // This part modified for DS ----------------------------- //
                var dimensionPoints = new ArrayList();                     //
                                                                           //
                //Add picked point by user                                 //
                dimensionPoints.Add(tPickedPoint);                         //
                                                                           //
                //Add bolt positions                                       //
                dimensionPoints.AddRange(tBoltGroup.BoltPositions);        //
                // ------------------------------------------------------- //

                //Set dimension direction
                T3D.Vector dimVector;
                var centerAxis = new Tekla.Structures.Geometry3d.Line(tBeam.StartPoint, tBeam.EndPoint);
                if (VectorsParallel(centerAxis.Direction, new T3D.Vector(0, 0, 1)))
                    dimVector = new T3D.Vector(0, 1, 0);
                else
                    dimVector = new T3D.Vector(centerAxis.Direction);
                var perpVector = dimVector.Cross(new T3D.Vector(0, 0, 1));

                //Add dimensions
                var newDimSet = new Tekla.Structures.Drawing.StraightDimensionSetHandler();
                newDimSet.CreateDimensionSet(tView, CovertArrayListToPointList(new ArrayList(dimensionPoints)), dimVector, distancePast);
                newDimSet.CreateDimensionSet(tView, CovertArrayListToPointList(new ArrayList(dimensionPoints)), perpVector, distancePast);

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            finally
            {
                new Model().GetWorkPlaneHandler().SetCurrentTransformationPlane(originalPlane);
            }
        }

        #region Helper Methods

        private static Tekla.Structures.Drawing.PointList CovertArrayListToPointList(ArrayList tArrayList)
        {
            // First point in point list is the origin of the dimension set.
            if (tArrayList.Count < 1) return null;
            var pList = new Tekla.Structures.Drawing.PointList();
            foreach (Tekla.Structures.Geometry3d.Point point in tArrayList)
                pList.Add(point);
            return pList;
        }
        private static bool VectorsParallel(Tekla.Structures.Geometry3d.Vector tVector1, Tekla.Structures.Geometry3d.Vector tVector2)
        {
            var v1 = VectorRound(tVector1).GetNormal();
            var v2 = VectorRound(tVector2).GetNormal();

            return Tekla.Structures.Geometry3d.Parallel.VectorToVector(v1, v2);
        }
        private static T3D.Vector VectorRound(Tekla.Structures.Geometry3d.Vector tVector1)
        {
            const int decPlace = 5;
            var x = Math.Round(tVector1.X, decPlace);
            var y = Math.Round(tVector1.Y, decPlace);
            var z = Math.Round(tVector1.Z, decPlace);
            return new T3D.Vector(x, y, z);
        }
        #endregion
    }
}
