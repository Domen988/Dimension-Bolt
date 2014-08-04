using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using Tekla.Structures.Drawing;
using TDWG = Tekla.Structures.Drawing;
using Tekla.Structures.Geometry3d;
using T3D = Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using Tekla.Structures.Model.Operations;
using Tekla.Structures.Solid;             
//////////
/////////
namespace Tekla.Technology.Akit.UserScript                                                                                /////////
{                                                                                                                         /////////
    public static class Script                                                                                            /////////
    {                                                                                                                     /////////
        public static void Run(Tekla.Technology.Akit.IScript akit)                                                        /////////
        {                                                                                                                 /////////
            try                                                                                                           /////////
            {                                                                                                             /////////
                new BoltDimension();                                                                                      /////////
                //new Example1();
            }                                                                                                             /////////
            catch (Exception)                                                                                             /////////
            { }                                                                                                           /////////
        }                                                                                                                 /////////
        static void Main(string[] args)                                                                                   /////////
        {                                                                                                                 /////////
            try                                                                                                           /////////
            {                                                                                                             /////////
                new BoltDimension();                                                                                      /////////
                //new Example1();
            }                                                                                                             /////////
            catch (Exception)                                                                                             /////////
            { }                                                                                                           /////////
        }                                                                                                                 /////////
    }                                                                                                                     /////////
                                                                                                                          /////////


    class BoltDimension                                                                                                   /////////
    {                                                                                                                     /////////
        /// <summary>                                                                                                     /////////
        /// rearranges array list in a way, that it starts with zero point                                                /////////
        /// </summary>                                                                                                    /////////
        /// <param name="alArrayList"></param>                                                                            /////////
        /// <returns> rearranged array list </returns>                                                                    /////////
        private static ArrayList setStartPoint(ArrayList alArrayList)                                                     /////////
        {                                                                                                                 /////////
            var al = new ArrayList();                                                                                     /////////
            var startX = ConvertArrayListToPointList(alArrayList)[0].X;                                                   /////////
            var startY = ConvertArrayListToPointList(alArrayList)[0].Y;                                                   //// ////
            //// ////
            foreach (Point pt in alArrayList)                                                                             //// ////
            {                                                                                                             //// ////
                double x = pt.X;                                                                                          ///   ///
                double y = pt.Y;                                                                                          //// ///
                /////////
                // change Math.Min -> Math.Max to set positions of zero point ----------------- //////////////////////////////////////////////////////////////////////
                startX = Math.Min(startX, x);                                                   //////////////////////////////////////////////////////////////////////
                startY = Math.Max(startY, y);                                                   //////////////////////////////////////////////////////////////////////
                // ---------------------------------------------------------------------------- //////////////////////////////////////////////////////////////////////
            }

            foreach (Point pt in alArrayList)
            {
                var tempPointsList = new ArrayList();
                if (pt.X == startX && pt.Y == startY)
                {
                    tempPointsList.Add(pt);
                    foreach (Point ptTemp in al)
                        tempPointsList.Add(ptTemp);
                    al = tempPointsList;
                }
                else
                    al.Add(pt);
            }
            return al;
        }


        /// <summary>
        /// Creates attributes for StraightDimensionSet. Makes dimensions 'relative', if there are only 2 different dimension values in the direction of dimension.
        /// Otherwise default 'absolute'.
        /// </summary>
        /// <param name="plPointList"></param>
        /// <param name="dimensionDirection"></param>
        /// <returns></returns>
        private static StraightDimensionSet.StraightDimensionSetAttributes getDimAtt(PointList plPointList, Vector dimensionDirection)
        {
            StraightDimensionSet.StraightDimensionSetAttributes att = new StraightDimensionSet.StraightDimensionSetAttributes((Tekla.Structures.Drawing.ModelObject)null);
            PointList tempPL = new PointList();
            tempPL.Add(plPointList[0]);
            if (dimensionDirection.Dot(new Vector(0, 1, 0)) != 0)
            {
                foreach (Point pt in plPointList)
                {
                    bool existsIndicator = false;
                    foreach (Point tempPt in tempPL)
                    {
                        if (tempPt.X == pt.X)
                        {
                            existsIndicator = true;                   // checks if point pt is allready included in tempPL point list
                            break;
                        }
                    }
                    if (existsIndicator == false)                     // if point pt is not included, add to tempPL point list
                        tempPL.Add(pt);
                }
                if (tempPL.Count < 3)                                 // count. if only two points with different values, make dimension relative
                    att.DimensionType = StraightDimensionSet.StraightDimensionSetAttributes.DimensionTypes.Relative;        ////////////////////////////////////////////////////////////////////////////////////////////////////////
                else                                                                                                        ////////////////////////////////////////////////////////////////////////////////////////////////////////
                    att.DimensionType = StraightDimensionSet.StraightDimensionSetAttributes.DimensionTypes.Absolute;        ////////////////////////////////////////////////////////////////////////////////////////////////////////
            }
            if (dimensionDirection.Dot(new Vector(1, 0, 0)) != 0)
            {
                foreach (Point pt in plPointList)
                {
                    bool existsIndicator = false;
                    foreach (Point tempPt in tempPL)
                    {
                        if (tempPt.Y == pt.Y)
                        {
                            existsIndicator = true;
                            break;
                        }
                    }
                    if (existsIndicator == false)
                        tempPL.Add(pt);
                }
                if (tempPL.Count < 3)
                    att.DimensionType = StraightDimensionSet.StraightDimensionSetAttributes.DimensionTypes.Relative;        ////////////////////////////////////////////////////////////////////////////////////////////////////////
                else                                                                                                        ////////////////////////////////////////////////////////////////////////////////////////////////////////
                    att.DimensionType = StraightDimensionSet.StraightDimensionSetAttributes.DimensionTypes.Absolute;        ////////////////////////////////////////////////////////////////////////////////////////////////////////
            }
            return att;
        }

        private string PickBoltGroup = "Pick bolt group to dimension.";
        private string PickPart = "Pick part or grid line to dimension bolts in relation to.";

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
            Tekla.Structures.Drawing.DrawingObject pickedObject1 = null;
            Tekla.Structures.Drawing.DrawingObject pickedObject2 = null;

            var picker = dh1.GetPicker();

            bool boltFlag = true;
            while (boltFlag)
            {
                picker.PickObject(PickBoltGroup, out pickedObject1, out viewBase1);
                if (pickedObject1 == null || viewBase1 == null || !(pickedObject1 is Tekla.Structures.Drawing.Bolt))
                {
                    PickBoltGroup = "No drawing bolt found for first picked object. Pick again.";
                }
                else
                    boltFlag = false;
            }

            var drawingBolt = pickedObject1 as Tekla.Structures.Drawing.Bolt;
            drawingBolt.Select();
            var modelBolt = new Model().SelectModelObject(drawingBolt.ModelIdentifier);

            //Get model part from picked object
            bool partFlag = true;
            while (partFlag)
            {
                picker.PickObject(PickPart, out pickedObject2, out viewBase2);
                if (pickedObject2 == null || viewBase2 == null || (!(pickedObject2 is Tekla.Structures.Drawing.Part) && !(pickedObject2 is Tekla.Structures.Drawing.GridLine)))
                {
                    PickPart = "No drawing part or grid line found for 2nd picked object. Pick again.";
                }
                else
                    partFlag = false;
            }

            //Get view from picked objects
            var tView = pickedObject1.GetView() as Tekla.Structures.Drawing.View;
            if (tView == null)
            {
                MessageBox.Show("Unable to find view for object picked.");
                return;
            }
            tView.Select();

            var drawingPart = pickedObject2 as Tekla.Structures.Drawing.Part;
            var drawingGridline = pickedObject2 as Tekla.Structures.Drawing.GridLine;
            if (pickedObject2 is Tekla.Structures.Drawing.Part)
            {
                drawingPart = pickedObject2 as Tekla.Structures.Drawing.Part;
                drawingPart.Select();
                var modelPart = new Model().SelectModelObject(drawingPart.ModelIdentifier);
                //Verify model part and bolt are not null
                if (modelPart == null || modelBolt == null)
                {
                    MessageBox.Show("Unable to get model object from drawing using Id's.");
                    return;
                }
                //Call specific methods for beams vs. contour plates
                if (modelPart is Tekla.Structures.Model.Beam)
                {
                    var tBeam = (Tekla.Structures.Model.Beam)modelPart;                                                   //
                    //string typeTest = "";                                                                               //
                    //string profile_type = "PROFILE_TYPE";                                                               // profile type test
                    //modelPart.GetReportProperty(profile_type, ref typeTest);                                            //
                    //MessageBox.Show(typeTest.ToString());                                                               //
                    var tBoltGroup = (Tekla.Structures.Model.BoltGroup)modelBolt;
                    CreateBoltDimensionToBeam(tView, tBeam, tBoltGroup);
                }
                else if (modelPart is Tekla.Structures.Model.ContourPlate)
                {
                    var tContourPlate = (Tekla.Structures.Model.ContourPlate)modelPart;
                    var tBoltGoup = (Tekla.Structures.Model.BoltGroup)modelBolt;
                    CreateBoltDimensionToPlate(tView, tContourPlate, tBoltGoup);
                }
            }

            else if (pickedObject2 is Tekla.Structures.Drawing.GridLine)
            {
                drawingGridline = pickedObject2 as Tekla.Structures.Drawing.GridLine;
                drawingGridline.Select();
                var modelGridline = new Model().SelectModelObject(drawingGridline.ModelIdentifier);
                //Verify model part and bolt are not null
                if (modelGridline == null || modelBolt == null)
                {
                    MessageBox.Show("Unable to get model object from drawing using Id's.");
                    return;
                }
                var tBoltGroup = (Tekla.Structures.Model.BoltGroup)modelBolt;
                CreateBoltDimensiontToGrid(tView, tBoltGroup);
            }
        }
        private static void CreateBoltDimensiontToGrid(Tekla.Structures.Drawing.View tView, Tekla.Structures.Model.BoltGroup tBoltGroup)
        {
            const double distancePast = 200.0;
            var originalPlane = new Model().GetWorkPlaneHandler().GetCurrentTransformationPlane();
            try
            {
                //Clear workplane to new blank global
                new Model().GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane());

                //Set model everything to the view
                new Model().GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane(tView.DisplayCoordinateSystem));

                DrawingObjectEnumerator allObjects = tView.GetAllObjects();
                Tekla.Structures.Drawing.Grid curGrid;
                GridLine curGridLine;
                DrawingObjectEnumerator allGridLines;
                var dimensionXGridPoints = new ArrayList();
                var dimensionYGridPoints = new ArrayList();
                var tolerance = 0.05;

                while (allObjects.MoveNext()) /* Iterate through all the objects in the view */
                {
                    if (allObjects.Current is Tekla.Structures.Drawing.Grid)  /* check if object is grid */
                    {
                        curGrid = allObjects.Current as Tekla.Structures.Drawing.Grid;
                        allGridLines = curGrid.GetObjects();
                        var point1 = new Point();
                        var point2 = new Point();
                        var pointForDim = new Point();
                        while (allGridLines.MoveNext())
                        {
                            if (allGridLines.Current is TDWG.GridLine)  /* Iterate through all the grid lines of the grid */
                            {
                                curGridLine = allGridLines.Current as TDWG.GridLine;
                                point1 = curGridLine.EndLabel.GridPoint;
                                point2 = curGridLine.StartLabel.GridPoint;
                                
                                if (point1.X - point2.X < tolerance)      // decide which points to take for which dimension
                                {
                                    pointForDim.X = point1.X;
                                }
                                if (point1.Y - point2.Y < tolerance)
                                {
                                    pointForDim.Y = point1.Y;
                                }

                            }
                        }
                        if (pointForDim.X == 0.0)
                        {
                            pointForDim.X = (point1.X + point2.X) / 2;
                        }
                        if (pointForDim.Y == 0.0)
                        {
                            pointForDim.Y = (point1.Y + point2.Y) / 2;
                        }
                        dimensionXGridPoints.Add(pointForDim);
                        dimensionYGridPoints.Add(pointForDim);
                    }
                }
                
                tBoltGroup.Select();
                tView.Select();

                //Get transformation matrix
                var transGlobal = new Model().GetWorkPlaneHandler().GetCurrentTransformationPlane().TransformationMatrixToGlobal;
                var transDisplayToView = MatrixFactory.ByCoordinateSystems(tView.DisplayCoordinateSystem, tView.ViewCoordinateSystem);

                //Add bolt positions
                var dimensionPoints = new ArrayList();
                dimensionPoints.AddRange(tBoltGroup.BoltPositions);

                //Create two new dimension lists
                ArrayList dimPointsX = new ArrayList();
                ArrayList dimPointsY = new ArrayList();
                dimPointsX.AddRange(dimensionPoints);
                dimPointsY.AddRange(dimensionPoints);

                dimPointsX.AddRange(dimensionXGridPoints);
                dimPointsY.AddRange(dimensionYGridPoints);

                //Set direction vectors
                Vector dimVector = new Vector(0, 1, 0);
                Vector perpVector = dimVector.Cross(new Vector(0, 0, 1));

                StraightDimensionSetHandler newDimSet = new Tekla.Structures.Drawing.StraightDimensionSetHandler();
                TDWG.StraightDimensionSet.StraightDimensionSetAttributes sdsa = new TDWG.StraightDimensionSet.StraightDimensionSetAttributes((TDWG.ModelObject)null);
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                sdsa.DimensionType = DimensionSetBaseAttributes.DimensionTypes.Relative;                                            //////////////////////////////////  Dimensioning to grid lines dimension type /////////////////////////////////////////////////////
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                newDimSet.CreateDimensionSet(tView, ConvertArrayListToPointList(dimPointsX), dimVector, distancePast, sdsa);
                newDimSet.CreateDimensionSet(tView, ConvertArrayListToPointList(dimPointsY), perpVector, distancePast, sdsa);
                
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
        private static void CreateBoltDimensionToPlate(Tekla.Structures.Drawing.View tView, Tekla.Structures.Model.ContourPlate tPlate,
            Tekla.Structures.Model.BoltGroup tBoltGroup)
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

                //Get transformation matrix
                var transGlobal = new Model().GetWorkPlaneHandler().GetCurrentTransformationPlane().TransformationMatrixToGlobal;
                var transDisplayToView = MatrixFactory.ByCoordinateSystems(tView.DisplayCoordinateSystem, tView.ViewCoordinateSystem);

                //Add bolt positions
                var dimensionPoints = new ArrayList();
                dimensionPoints.AddRange(tBoltGroup.BoltPositions);
                Solid tSolid = tPlate.GetSolid();

                //Create two new dimension lists
                ArrayList dimPointsX = new ArrayList();
                ArrayList dimPointsY = new ArrayList();
                dimPointsX.AddRange(dimensionPoints);
                dimPointsY.AddRange(dimensionPoints);

                Point maxPoint = tSolid.MaximumPoint;
                Point minPoint = tSolid.MinimumPoint;

                //Add solid Max end dimensions
                if (tView.RestrictionBox.IsInside(transDisplayToView.Transform(tSolid.MaximumPoint)))
                {
                    //X direction
                    ArrayList intersectXList =
                        tSolid.Intersect(new LineSegment(maxPoint, new Point(maxPoint.X, maxPoint.Y + 100, maxPoint.Z)));
                    if (intersectXList.Count > 0)
                        dimPointsX.Add(intersectXList[0] as Point);

                    //Y direction
                    ArrayList intersectYList =
                        tSolid.Intersect(new LineSegment(maxPoint, new Point(maxPoint.X + 100, maxPoint.Y, maxPoint.Z)));
                    if (intersectYList.Count > 0)
                        dimPointsY.Add(intersectYList[0] as Point);
                }

                //Add solid Min end dimensions
                if (tView.RestrictionBox.IsInside(transDisplayToView.Transform(tSolid.MinimumPoint)))
                {
                    //X direction
                    ArrayList intersectXList =
                        tSolid.Intersect(new LineSegment(minPoint, new Point(minPoint.X, minPoint.Y + 100, minPoint.Z)));
                    if (intersectXList.Count > 0)
                        dimPointsX.Add(intersectXList[0] as Point);

                    //Y direction
                    ArrayList intersectYList =
                        tSolid.Intersect(new LineSegment(minPoint, new Point(minPoint.X + 100, minPoint.Y, minPoint.Z)));
                    if (intersectYList.Count > 0)
                        dimPointsY.Add(intersectYList[0] as Point);//xxx
                }

                //Set direction vectors
                Vector dimVector = new Vector(0, 1, 0);
                Vector perpVector = dimVector.Cross(new Vector(0, 0, 1));

                StraightDimensionSetHandler newDimSet = new Tekla.Structures.Drawing.StraightDimensionSetHandler();
                newDimSet.CreateDimensionSet(tView, ConvertArrayListToPointList(dimPointsX), dimVector, distancePast);
                newDimSet.CreateDimensionSet(tView, ConvertArrayListToPointList(dimPointsY), perpVector, distancePast);

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
                        Tekla.Structures.Model.BoltGroup tBoltGroup)
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

                //Get transformation matrix
                var transDisplayToView = MatrixFactory.ByCoordinateSystems(tView.DisplayCoordinateSystem, tView.ViewCoordinateSystem);

                //create dimension point array list
                var dimensionPoints = new ArrayList();

                // SKA ------ get positive beam Y direction in view coordinates
                T3D.CoordinateSystem csBeam = tBeam.GetCoordinateSystem();
                Point ptBeamY = csBeam.AxisY;
                Vector vcBeamY = new Vector(ptBeamY);
                Point ptBeamOrigin = csBeam.Origin;

                //Add solid edge points if in side view restriction box
                Solid tSolid = tBeam.GetSolid(Solid.SolidCreationTypeEnum.FITTED);
                PointList vertexList = GetVertexList(tSolid);
                Trace.WriteLine(vertexList.Count + " Points found.");

                //Set dimension direction
                Vector dimVector;
                var centerAxis = new Tekla.Structures.Geometry3d.Line(tBeam.StartPoint, tBeam.EndPoint);
                bool beamPerpenicularToPlane = true;
                if (VectorsParallel(centerAxis.Direction, new Vector(0, 0, 1)))
                    dimVector = new Vector(0, 1, 0);
                else
                {
                    dimVector = new Vector(centerAxis.Direction);
                    beamPerpenicularToPlane = false;                                                   // if beam is not perpendicular change to false
                }
                var perpVector = dimVector.Cross(new Vector(0, 0, 1));

                foreach (Point pt in vertexList)
                {
                    //Get point to check
                    var viewCsPoint = transDisplayToView.Transform(pt);

                    //Check if local point is in view by 2 coord syst
                    if (beamPerpenicularToPlane == false)
                        if (!tView.RestrictionBox.IsInside(viewCsPoint)) continue;
                    dimensionPoints.Add(pt);
                    Trace.WriteLine("View ViewCs by 2 coord sys point found in view");
                }

                CoordinateSystem csB = tBoltGroup.GetCoordinateSystem();
                clBoltSide sideCheck = oneSideCheck(tBoltGroup.BoltPositions, ptBeamOrigin);         // do a check if whole bolt group is on one side of beams x and y axis

                //Create dimensions
                StraightDimensionSetHandler newDimSet = new Tekla.Structures.Drawing.StraightDimensionSetHandler();

                if (beamPerpenicularToPlane == false)                                                // if beam is not perpendicular, dimension to all beam points in view
                {
                    foreach (Point pt in tBoltGroup.BoltPositions)
                        dimensionPoints.Add(pt);
                    newDimSet.CreateDimensionSet(tView, ConvertArrayListToPointList(new ArrayList(dimensionPoints)), dimVector, distancePast);
                    newDimSet.CreateDimensionSet(tView, ConvertArrayListToPointList(new ArrayList(dimensionPoints)), perpVector, distancePast);
                    return;
                }

                var dimPointList = getDimensionPoints(                                               // get dimension points for x direction
                        new ArrayList(dimensionPoints),
                        new ArrayList(tBoltGroup.BoltPositions),
                        new Vector(vcBeamY),
                        new Point(ptBeamOrigin),
                        sideCheck,
                        dimVector);
                var dimAtt = getDimAtt(dimPointList, dimVector);                                      // get dimension attributes for x direction

                var perpPointList = getDimensionPoints(                                               // get dimension points for y direction
                        new ArrayList(dimensionPoints),
                        new ArrayList(tBoltGroup.BoltPositions),
                        new Vector(vcBeamY),
                        new Point(ptBeamOrigin),
                        sideCheck,
                        perpVector);
                var perpAtt = getDimAtt(perpPointList, perpVector);                                   // get dimension attributes for y direction

                newDimSet.CreateDimensionSet(                                                         // create dimension in x direction
                    tView,
                    dimPointList,
                    dimVector,
                    distancePast,
                    dimAtt);

                newDimSet.CreateDimensionSet(                                                         // create dimension in y direction
                    tView,
                    perpPointList,
                    perpVector,
                    distancePast,
                    perpAtt);
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

        #region Dimensioning logic methods
        /// <summary>
        /// Checks if bolt positions of bolt group are all on the same sides of central axes
        /// </summary>
        /// <param name="BoltPositions"></param>
        /// <param name="ptBeamOrigin"></param>
        /// <returns> object of class clBoltSide - it consist of two doubles: sideInX, sideInY </returns>
        private static clBoltSide oneSideCheck(ArrayList BoltPositions, Point ptBeamOrigin)
        {
            clBoltSide result = new clBoltSide();
            double sideTestX = 0;
            double sideTestY = 0;
            // multiplies point's xses (or ypsilons) between themselves. If product is negative bolt positions are on different side of center axis.
            bool sideTestXZero = false;
            bool sideTestYZero = false;
            foreach (Tekla.Structures.Geometry3d.Point point in BoltPositions)
            {
                Point tempP = new Point();
                tempP = point - ptBeamOrigin;

                sideTestX *= tempP.X;

                if (sideTestXZero == false)
                {
                    if (sideTestX < 0 || tempP.X == 0)
                    {
                        sideTestX = 0;
                        sideTestXZero = true;
                    }
                    else
                        sideTestX = tempP.X;
                }

                sideTestY *= tempP.Y;
                if (sideTestYZero == false)
                {
                    if (sideTestY < 0 || tempP.Y == 0)
                    {
                        sideTestY = 0;
                        sideTestYZero = true;
                    }
                    else
                        sideTestY = tempP.Y;
                }
            }
            if (sideTestX != 0)
                sideTestX /= Math.Abs(sideTestX);

            if (sideTestY != 0)
                sideTestY /= Math.Abs(sideTestY);

            result.sideInX = sideTestX;
            result.sideInY = sideTestY;
            return result;
        }
        /// <summary>
        /// Makes the logical decisions for dimensioning.
        /// </summary>
        /// <param name="alArrayList"> List of beam contour points. </param>
        /// <param name="alBoltPositions"> List of bolt positions. </param>
        /// <param name="vcBeamY"> Vector of beams weak axis. </param>
        /// <param name="ptBeamOrigin"> Beam origin point. </param>
        /// <param name="BoltSideCheck"> Result of bolt side check. </param>
        /// <param name="dimensionDirection"> Vector of dimension direction. </param>
        /// <returns> PointList with points for dimensioning. </returns>
        private static PointList getDimensionPoints(ArrayList alArrayList, ArrayList alBoltPositions, Vector vcBeamY, Point ptBeamOrigin, clBoltSide BoltSideCheck, Vector dimensionDirection)
        {
            if (alArrayList.Count < 1) return null;
            var ePointsList = new Tekla.Structures.Drawing.PointList();
            var BoltPoints = ConvertArrayListToPointList(alBoltPositions);

            // check for axis orientation
            if (vcBeamY.Dot(dimensionDirection) != 0)                           // check if dimension is parallel to weak axis
            {
                // do if: 1) dimension is horizontal (parallel with (0, 1, 0) - Teklas mistake??), 2) bolt group is on one side in x direction (+x or -x)
                if (dimensionDirection.Dot(new Vector(0, 1, 0)) * BoltSideCheck.sideInX != 0)
                {
                    ePointsList = dimensionWeakAxis(alArrayList, alBoltPositions, ptBeamOrigin, BoltSideCheck.sideInX, "X");
                    ePointsList = addBoltPoints(ePointsList, alBoltPositions);
                    return ePointsList;
                }
                // do if: 1) dimension is vertical (parallel with (1, 0, 0)), 2) bolt group is on one side in y direction (+y or -y)
                if (dimensionDirection.Dot(new Vector(1, 0, 0)) * BoltSideCheck.sideInY != 0)
                {
                    ePointsList = dimensionWeakAxis(alArrayList, alBoltPositions, ptBeamOrigin, BoltSideCheck.sideInY, "Y");
                    ePointsList = addBoltPoints(ePointsList, alBoltPositions);
                    return ePointsList;
                }
            }

            if (vcBeamY.Dot(dimensionDirection) == 0)
            {
                if (dimensionDirection.Dot(new Vector(0, 1, 0)) * BoltSideCheck.sideInX != 0)
                {
                    ePointsList = dimensionStrongAxis(alArrayList, alBoltPositions, ptBeamOrigin, BoltSideCheck.sideInX, "X", vcBeamY, dimensionDirection);
                    ePointsList = addBoltPoints(ePointsList, alBoltPositions);
                    return ePointsList;
                }
                if (dimensionDirection.Dot(new Vector(1, 0, 0)) * BoltSideCheck.sideInY != 0)
                {
                    ePointsList = dimensionStrongAxis(alArrayList, alBoltPositions, ptBeamOrigin, BoltSideCheck.sideInY, "Y", vcBeamY, dimensionDirection);
                    ePointsList = addBoltPoints(ePointsList, alBoltPositions);
                    return ePointsList;
                }
            }

            // if method didn't return by now, it (usually) means, that bolt group is placed on both sides of central axis. 
            // Do dimensioning to extreme points of beam profile.
            ePointsList = dimensionToExtremePoints(alArrayList, alBoltPositions, ptBeamOrigin, vcBeamY, dimensionDirection);                     // this method adds bolt positions by itself
            ePointsList = addBoltPoints(ePointsList, alBoltPositions);
            return ePointsList;
        }
        /// <summary>
        /// add bolt position points to ePointsList
        /// </summary>
        /// <param name="ePointsList"></param>
        /// <param name="boltPositions"></param>
        /// <param name="ptBeamOrigin"></param>
        /// <returns> new pointList with added bolt positions </returns>
        private static ArrayList addBoltPoints(ArrayList ePointsList, ArrayList boltPositions)
        {
            foreach (Point pt in boltPositions)
                ePointsList.Add(pt);
            return ePointsList;
        }

        private static PointList addBoltPoints(PointList ePointsList, ArrayList boltPositions)
        {
            foreach (Point pt in boltPositions)
                ePointsList.Add(pt);
            return ePointsList;
        }
        /// <summary>
        /// Dimensions to etreme points of beam's conotur. Sets the dimension start.
        /// </summary>
        /// <param name="alArrayList"></param>
        /// <param name="alBoltPositions"></param>
        /// <param name="ptBeamOrigin"></param>
        /// <returns></returns>
        private static PointList dimensionToExtremePoints(ArrayList alArrayList, ArrayList alBoltPositions, Point ptBeamOrigin, Vector vcBeamY, Vector vcDimensionDirection)
        {
            var ePointsList = new Tekla.Structures.Drawing.PointList();
            var tempArrayList = new ArrayList();
            var maxDistance = new double();
            var startX = ConvertArrayListToPointList(alArrayList)[0].X;
            var startY = ConvertArrayListToPointList(alArrayList)[0].Y;

            foreach (Point pt in alArrayList)
            {
                double distance = Math.Pow(pt.X - ptBeamOrigin.X, 2) + Math.Pow(pt.Y - ptBeamOrigin.Y, 2);
                maxDistance = Math.Max(maxDistance, distance);
            }

            foreach (Point pt in alArrayList)
            {
                if ((Math.Pow(pt.X - ptBeamOrigin.X, 2) + Math.Pow(pt.Y - ptBeamOrigin.Y, 2)) == maxDistance)
                    tempArrayList.Add(pt);
            }

            tempArrayList = setDimensionSide(tempArrayList, alBoltPositions, ptBeamOrigin, vcBeamY, vcDimensionDirection);
            tempArrayList = setStartPoint(tempArrayList);
            ePointsList = ConvertArrayListToPointList(tempArrayList);
            return ePointsList;
        }
        /// <summary>
        /// Checks if all bolts are outside 
        /// set all dimension start points on beam on the side of the bolt group
        /// </summary>
        /// <param name="alArrayList"></param>
        /// <param name="alBoltPositions"></param>
        /// <returns></returns>
        private static ArrayList setDimensionSide(ArrayList alArrayList, ArrayList alBoltPositions, Point ptBeamOrigin, Vector vcBeamY, Vector vcDimensionDirection)
        {
            var maxX = ConvertArrayListToPointList(alArrayList)[0].X;
            var minX = ConvertArrayListToPointList(alArrayList)[0].X;
            var maxY = ConvertArrayListToPointList(alArrayList)[0].Y;
            var minY = ConvertArrayListToPointList(alArrayList)[0].Y;
            bool boolMaxX = true;
            bool boolMinX = true;
            bool boolMaxY = true;
            bool boolMinY = true;
            ArrayList alX = new ArrayList();
            ArrayList alXY = new ArrayList();

            foreach (Point pt in alArrayList)
            {
                maxX = Math.Max(maxX, pt.X);
                minX = Math.Min(minX, pt.X);
                maxY = Math.Max(maxY, pt.Y);
                minY = Math.Min(minY, pt.Y);
            }

            if (Vector.Dot(vcBeamY, vcDimensionDirection) != 0)
            {
                foreach (Point pt in alBoltPositions)
                {
                    if (pt.X <= maxX)
                        boolMaxX = false;
                    if (pt.X >= minX)
                        boolMinX = false;

                    if (pt.Y <= maxY)
                        boolMaxY = false;
                    if (pt.Y >= minY)
                        boolMinY = false;
                }

                foreach (Point pt in alArrayList)
                {
                    if (boolMaxX == true && pt.X == maxX)
                        alX.Add(pt);
                    else if (boolMinX == true && pt.X == minX)
                        alX.Add(pt);
                    else if (boolMaxX == false && boolMinX == false)
                        alX.Add(pt);
                }
                foreach (Point pt in alX)
                {
                    if (boolMaxY == true && pt.Y == maxY)
                        alXY.Add(pt);
                    else if (boolMinY == true && pt.Y == minY)
                        alXY.Add(pt);
                    else if (boolMaxY == false && boolMinY == false)
                        alXY.Add(pt);
                }
            }
            else
            {
                foreach (Point pt in alBoltPositions)
                {
                    if (Vector.Dot(new Vector(0, 1, 0), vcDimensionDirection) == 0)
                    {
                        if (pt.X <= ptBeamOrigin.X)
                            boolMaxX = false;
                        if (pt.X >= ptBeamOrigin.X)
                            boolMinX = false;
                    }


                    if (Vector.Dot(new Vector(1, 0, 0), vcDimensionDirection) == 0)
                    {
                        if (pt.Y <= ptBeamOrigin.Y)
                            boolMaxY = false;
                        if (pt.Y >= ptBeamOrigin.Y)
                            boolMinY = false;
                    }

                }

                foreach (Point pt in alArrayList)
                {
                    if (boolMaxX == true && pt.X == maxX)
                        alX.Add(pt);
                    else if (boolMinX == true && pt.X == minX)
                        alX.Add(pt);
                    else if (boolMaxX == false && boolMinX == false)
                        alX.Add(pt);
                }
                foreach (Point pt in alX)
                {
                    if (boolMaxY == true && pt.Y == maxY)
                        alXY.Add(pt);
                    else if (boolMinY == true && pt.Y == minY)
                        alXY.Add(pt);
                    else if (boolMaxY == false && boolMinY == false)
                        alXY.Add(pt);
                }
            }



            return alXY;

        }

        /// <summary>
        /// Create dimension in weak axis. If at least one bolt is between flanges in strong direction, dimension to web.
        /// </summary>
        /// <param name="alArrayList"></param>
        /// <param name="alBoltPositions"></param>
        /// <param name="side"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        private static PointList dimensionWeakAxis(ArrayList alArrayList, ArrayList alBoltPositions, Point ptBeamOrigin, double side, string direction)
        {
            var aListA = new ArrayList();
            var aListB = new ArrayList();
            var ePointsList = new Tekla.Structures.Drawing.PointList();

            string antiDirection = "";
            if (direction == "X")
                antiDirection = "Y";
            else if (direction == "Y")
                antiDirection = "X";

            // dimension to weak axis need to check, if bolt falls out of beam bounding box in strong axis. 
            Point startPoint = ConvertArrayListToPointList(alArrayList)[0];
            var minValue = (double)(startPoint.GetType().GetField(antiDirection).GetValue(startPoint));
            var maxValue = (double)(startPoint.GetType().GetField(antiDirection).GetValue(startPoint));

            foreach (Point pt in alArrayList)
            {
                minValue = Math.Min(minValue, (double)(pt.GetType().GetField(antiDirection).GetValue(pt)));
                maxValue = Math.Max(maxValue, (double)(pt.GetType().GetField(antiDirection).GetValue(pt)));
            }

            Point startBolt = ConvertArrayListToPointList(alBoltPositions)[0];
            var minBoltValue = (double)(startBolt.GetType().GetField(antiDirection).GetValue(startBolt));
            var maxBoltValue = (double)(startBolt.GetType().GetField(antiDirection).GetValue(startBolt));
            foreach (Point pt in alBoltPositions)
            {
                Point tempPoint = new Point();
                tempPoint.GetType().GetField(antiDirection).SetValue(tempPoint, (double)(pt.GetType().GetField(antiDirection).GetValue(pt)));
                if ((double)(minValue) <= (double)(tempPoint.GetType().GetField(antiDirection).GetValue(tempPoint)) && (double)(tempPoint.GetType().GetField(antiDirection).GetValue(tempPoint)) <= (double)(maxValue))
                {
                    // if at least one bolt in bolt positions is inside of beams outer points in strong axis, do dimensioning to web.
                    ePointsList = dimWAToWeb(alArrayList, alBoltPositions, side, direction);
                    return ePointsList;
                }
                minBoltValue = Math.Min(minBoltValue, (double)(tempPoint.GetType().GetField(antiDirection).GetValue(tempPoint)));
                maxBoltValue = Math.Max(maxBoltValue, (double)(tempPoint.GetType().GetField(antiDirection).GetValue(tempPoint)));
            }
            // if all bolts fall outside of beams outer points in strong axis, do dimensioning to extreme points
            if (maxBoltValue < minValue)
            {
                foreach (Point pt in alArrayList)
                {
                    if ((double)(pt.GetType().GetField(antiDirection).GetValue(pt)) == (double)(minValue))
                        aListA.Add(pt);
                }
                aListA = setStartPoint(aListA);
                ePointsList = ConvertArrayListToPointList(aListA);
                return ePointsList;
            }
            if (minBoltValue > (double)(maxValue))
            {
                foreach (Point pt in alArrayList)
                {
                    var test = pt.GetType().GetField(antiDirection).GetValue(pt);
                    if ((double)(test) == (double)(maxValue))
                        aListA.Add(pt);
                }
                aListA = setStartPoint(aListA);
                ePointsList = ConvertArrayListToPointList(aListA);
                return ePointsList;
            }

            return ePointsList;
        }
        /// <summary>
        /// Dimension to web in weak direction.
        /// </summary>
        /// <param name="alArrayList"></param>
        /// <param name="alBoltPositions"></param>
        /// <param name="side"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        private static PointList dimWAToWeb(ArrayList alArrayList, ArrayList alBoltPositions, double side, string direction)
        {
            var aListA = new ArrayList();
            var aListB = new ArrayList();
            var ePointsList = new Tekla.Structures.Drawing.PointList();
            var minValue = new double();

            Point zeroPoint = ConvertArrayListToPointList(alArrayList)[0];
            minValue = Math.Abs((double)(zeroPoint.GetType().GetField(direction).GetValue(zeroPoint)));
            // search for point with minimum value on the side specified with "side" (side of bolt group).
            foreach (Point pt in alArrayList)
            {
                if ((double)(pt.GetType().GetField(direction).GetValue(pt)) * side < 0)
                    continue;
                minValue = Math.Min(minValue, Math.Abs((double)(pt.GetType().GetField(direction).GetValue(pt))));
                aListA.Add(pt);
            }
            // add point to list, if it's value is minimum.
            foreach (Point pt in aListA)
            {
                if (Math.Abs((double)(pt.GetType().GetField(direction).GetValue(pt))) == (double)(minValue))
                    aListB.Add(pt);
            }

            ePointsList = ConvertArrayListToPointList(aListB);
            return ePointsList;
        }
        /// <summary>
        /// Dimension in strong axis. Check if at least one bolt is between tha flanges on strong direction and dimension accordingly.
        /// </summary>
        /// <param name="tArrayList"></param>
        /// <param name="boltPositions"></param>
        /// <param name="ptBeamOrigin"></param>
        /// <param name="side"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        private static PointList dimensionStrongAxis(ArrayList tArrayList, ArrayList boltPositions, Point ptBeamOrigin, double side, string direction, Vector vcBeamY, Vector vcDimensionDirection)
        {
            var aListA = new ArrayList();
            var aListB = new ArrayList();
            var ePointsList = new Tekla.Structures.Drawing.PointList();
            var minValue = new double();
            var maxValue = new double();
            var minBoltValue = new double();
            var maxBoltValue = new double();

            Point zeroPoint = ConvertArrayListToPointList(tArrayList)[0];

            minValue = (double)(zeroPoint.GetType().GetField(direction).GetValue(zeroPoint));
            maxValue = (double)(zeroPoint.GetType().GetField(direction).GetValue(zeroPoint));

            foreach (Point pt in tArrayList)
            {
                minValue = Math.Min(minValue, (double)(pt.GetType().GetField(direction).GetValue(pt)));
                maxValue = Math.Max(maxValue, (double)(pt.GetType().GetField(direction).GetValue(pt)));
            }

            Point zeroBolt = ConvertArrayListToPointList(boltPositions)[0];
            minBoltValue = (double)(zeroBolt.GetType().GetField(direction).GetValue(zeroBolt));
            maxBoltValue = (double)(zeroBolt.GetType().GetField(direction).GetValue(zeroBolt));

            foreach (Point pt in boltPositions)
            {
                Point tempPoint = new Point();
                tempPoint = pt;// +ptBeamOrigin.X;
                // dimension to extreme points, if at least one bolt position is between the flanges in strong direction.
                if (minValue <= (double)(tempPoint.GetType().GetField(direction).GetValue(tempPoint)) && (double)(tempPoint.GetType().GetField(direction).GetValue(tempPoint)) <= maxValue)
                {
                    ePointsList = dimensionToExtremePoints(tArrayList, boltPositions, ptBeamOrigin, vcBeamY, vcDimensionDirection);
                    return ePointsList;
                }
                minBoltValue = Math.Min(minBoltValue, (double)(tempPoint.GetType().GetField(direction).GetValue(tempPoint)));
                maxBoltValue = Math.Max(maxBoltValue, (double)(tempPoint.GetType().GetField(direction).GetValue(tempPoint)));
            }
            // dimension to near flange, if all bolts are outside flange in strong direction
            if (maxBoltValue < minValue)
            {
                foreach (Point pt in tArrayList)
                {
                    if ((double)(pt.GetType().GetField(direction).GetValue(pt)) == (double)(minValue))
                        aListA.Add(pt);
                }
                ePointsList = ConvertArrayListToPointList(aListA);
                return ePointsList;
            }
            if (minBoltValue > maxValue)
            {
                foreach (Point pt in tArrayList)
                {
                    if ((double)(pt.GetType().GetField(direction).GetValue(pt)) == (double)(maxValue))
                        aListA.Add(pt);
                }
                ePointsList = ConvertArrayListToPointList(aListA);
                return ePointsList;
            }
            return ePointsList;
        }

        #endregion

        #region Helper Methods
        private static PointList GetVertexList(Solid tSolid)
        {
            var faceEnum = tSolid.GetFaceEnumerator();
            var vertexList = new PointList();
            while (faceEnum.MoveNext())
            {
                var face = faceEnum.Current as Face;
                if (face == null) continue;

                var loops = face.GetLoopEnumerator();
                while (loops.MoveNext())
                {
                    var lp = loops.Current as Loop;
                    if (lp == null) continue;

                    var vertices = lp.GetVertexEnumerator();
                    while (vertices.MoveNext())
                        vertexList.Add(vertices.Current as Point);
                }
            }
            return vertexList;
        }
        /// <summary>
        /// checks if bolts are positioned only on one side of the beam.
        /// </summary>
        /// <param name="alBoltPositions"> positions of bolts in view coordinates </param>
        /// <param name="ptBeamOrigin"> norigin of beam in view coordinates </param>
        /// <returns> two doubles with values 1, 0, -1. 0 Stands for symetric bolt positions or one bolt at zero. 
        /// 1 and -1 means that whole bolt group is on one side and tells which side is that. </returns>
        private class clBoltSide
        {
            public double sideInX;
            public double sideInY;
        }
        private static Tekla.Structures.Drawing.PointList ConvertArrayListToPointList(ArrayList tArrayList)
        {
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
        private static Vector VectorRound(Tekla.Structures.Geometry3d.Vector tVector1)
        {
            const int decPlace = 5;
            var x = Math.Round(tVector1.X, decPlace);
            var y = Math.Round(tVector1.Y, decPlace);
            var z = Math.Round(tVector1.Z, decPlace);
            return new Vector(x, y, z);
        }
        #endregion
    }
}
