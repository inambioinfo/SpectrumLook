﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using ZedGraph;
using System.Windows.Forms;
using SpectrumLook.Builders;

namespace SpectrumLook.Views
{
    /// <summary>
    /// Delegate used so that the plot can update the value of the snaping cursor position.
    /// </summary>
    /// <param name="newPosition"></param>
    public delegate void UpdatePointDelegate(PointF newPosition); 

    public class MyZedGraph : ZedGraph.ZedGraphControl
    {
        private int m_snapBoxSize = 5;
        private Color m_snapBoxColor = Color.Black;
        private Point m_snapBoxPosition;
        private bool m_snapShowing;
        public bool m_arrowShowing;
        public PointF m_arrowPoint;
        private PointPairList m_unmatchedPoints;
        private PointPairList m_matchedPoints;
        private string m_currentPeptide;
        private string m_currentScanNumber;
        public PlotOptions m_options;
        public UpdatePointDelegate m_updateCursorCallback;
        public Manager m_manager;

        private const string unmatchedCurveName = "Unmatched Peaks";
        private const string matchedCurveName = "Matched Peaks";

        #region Initialization
        /// <summary>
        /// The construtor for our custom ZedGraph
        /// </summary>
        public MyZedGraph()
        {
            m_snapBoxPosition.X = 0;
            m_snapBoxPosition.Y = 0;

            ZoomEvent += new ZoomEventHandler(MyZedGraph_ZoomEvent);
            Resize += new EventHandler(MyZedGraph_Resize);
            InitializeGraph();
        }

        /// <summary>
        /// Sets the Title, X, and Y axis text in the graph
        /// </summary>
        /// <param name="zgc"></param>
        private void InitializeGraph()
        {
            this.GraphPane.Title.Text = "Scan: ";
            this.GraphPane.XAxis.Title.Text = "m/z";
            this.GraphPane.YAxis.Title.Text = "Relative Intensity";
        }

        /// <summary>
        /// Sets the size for the graph in proportion to the Form that it is in
        /// </summary>
        public void SetSize(Point position, int width, int height)
        {
            //the top right corner location of the graph
            this.Location = position;
            
            this.Size = new Size(width, height);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MyZedGraph
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.Name = "MyZedGraph";
            this.Size = new System.Drawing.Size(151, 150);
            this.ResumeLayout(false);

        }
        #endregion

        #region Snapping Cursor
        /// <summary>
        /// zedgraph event that fires when the mouse moves.  We use this to paint the snapping cursor over the graph.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (m_options.showSnappingCursor)
            {
                PointF mousePosition = new PointF(e.X, e.Y); 
                UpdateSnappingCursor(mousePosition);
            }
        }

        public void UpdateSnappingCursor(PointF mousePosition)
        {
            GraphPane closestPane;
            PointPair closestPoint = null;

            if (FindClosestPoint(mousePosition, out closestPoint, out closestPane))
            {
                PointF graphPoint = new PointF((float)closestPoint.X, (float)closestPoint.Y);
                PointF boxPoint = closestPane.GeneralTransform(graphPoint, CoordType.AxisXYScale);
                m_snapBoxPosition.X = (int)boxPoint.X - (m_snapBoxSize / 2);
                m_snapBoxPosition.Y = (int)boxPoint.Y - (m_snapBoxSize / 2);

                if (m_updateCursorCallback != null)
                {
                    m_updateCursorCallback(graphPoint);
                }

                m_snapShowing = true;
                this.Invalidate();
            }
            else
            {
                if (m_snapShowing)
                {
                    m_snapShowing = false;
                    Invalidate();
                }
            }
        }
        
        /// <summary>
        /// Finds the closest point in the plot to the mouse position and assigns it to closestPoint
        /// </summary>
        /// <param name="mousePosition">The current mouse position</param>
        /// <param name="closestPoint">will either be the closest point or an empty point if one is not found</param>
        /// <returns>True if a point is found, False otherwise</returns>
        public bool FindClosestPoint(PointF mousePosition, out PointPair closestPoint, out GraphPane closestPane)
        {
            double graphX;
            double graphY;
            closestPoint = null;
            closestPane = null;

            PointPair unmatchedClosest = new PointPair();
            PointPair matchedClosest = new PointPair();

            closestPane = MasterPane.FindPane(mousePosition);

            if (closestPane == null)
            {  //if we couldn't get the pane then we can't find the point
                return false; 
            }

            // reverseTransform converts the mouse position to the point on a graph
            closestPane.ReverseTransform(mousePosition, out graphX, out graphY);

            PointPair mousePositionPP = new PointPair(graphX, graphY);

            matchedClosest = GetClosestPointInCurve(mousePositionPP, closestPane.CurveList[matchedCurveName]);
            if (!m_options.hideUnmatched)
            {
                unmatchedClosest = GetClosestPointInCurve(mousePositionPP, closestPane.CurveList[unmatchedCurveName]);
            }

            //determine which of the points we found was closer
            if (CalculateDistance(mousePositionPP, matchedClosest) > CalculateDistance(mousePositionPP, unmatchedClosest))
            {
                //unmatched is closest
                closestPoint = unmatchedClosest;
                m_snapBoxColor = m_options.unmatchedColor;
            }
            else
            {
                //matched is closest
                closestPoint = matchedClosest;
                m_snapBoxColor = m_options.matchedColor;
            }

            bool foundClosest = (closestPoint.X != 0 || closestPoint.Y != 0);

            return foundClosest;
        }
        
        /// <summary>
        /// Retrieves the closest point to the mouse point in the curve
        /// </summary>
        private PointPair GetClosestPointInCurve(PointPair mousePoint, CurveItem curve)
        {
            PointPair closestPoint = new PointPair();
            if (curve != null)
            {
                IPointList toSearch = curve.Points;
                double closestDistance = GraphPane.XAxis.Scale.Max;

                if (toSearch != null && toSearch.Count > 0)
                {
                    for (int i = 0; i < toSearch.Count; i++)
                    {
                        PointPair point = toSearch[i];
                        double tempDist = CalculateDistance(mousePoint, point);
                        if (tempDist < closestDistance)
                        {
                            closestDistance = tempDist;
                            closestPoint = point;
                        }
                    }
                }
            }

            return closestPoint;
        }

        /// <summary>
        /// Simply Calculates the distance between the two points on the XY plane
        /// </summary>
        private double CalculateDistance(PointPair pointA, PointPair pointB)
        {
            return Math.Sqrt(Math.Pow(pointB.X - pointA.X, 2) + Math.Pow(pointB.Y - pointA.Y, 2));
        }

        /// <summary>
        /// zedgraph event that fires the moment when the mouse is no longer above the form.  We use this to know when to stop painting the snapping cursor
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            m_snapShowing = false;
            this.Invalidate();
        }

        /// <summary>
        /// Draws the Snapping cursor at xcoord, ycoord
        /// </summary>
        /// <param name="xcoord">The x coordinate on the form where the snapping point will go</param>
        /// <param name="ycoord">The y coordinate on the form where the snapping point will go</param>
        public void DrawSnapCursor(Point boxPosition, PaintEventArgs e)
        {
            SolidBrush snapBrush = new SolidBrush(m_snapBoxColor);
            Rectangle snapRect = new Rectangle(boxPosition.X, boxPosition.Y, m_snapBoxSize, m_snapBoxSize);

            e.Graphics.FillRectangle(snapBrush, snapRect);
        }

        /// <summary>
        /// Returns the position of the snapping cursor for other methods to use
        /// </summary>
        public Point GetSnapCursorPosition()
        {
            return m_snapBoxPosition;
        }
        #endregion

        #region Annotations
        /// <summary>
        /// Double click event for the graph when we want to edit annotations
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);

            //edit annotation?
            //System.Windows.Forms.MessageBox.Show("Edit annotations not implemented yet");
        }
        
        /// <summary>
        /// Adds annotations from the matchedPoints to the Graph Pane
        /// Plot Options specify how much of the top % of annotations to display, along with size and color.
        /// </summary>
        /// <param name="pointsList"></param>
        /// <param name="myPane"></param>
        private void AddAnnotations(IPointList pointsList, GraphPane myPane)
        {
            //add the annotations for the matched items
            double offset = 5;
            double minIntensityToDisplay = FindMinIntensityToDisplay(pointsList);
            LadderInstance currentInstance = m_manager.GetCurrentInstance();

            for (int i = 0; i < pointsList.Count; i++)
            {
                bool usingCustomAnnotation = false;
                TextObj text = new TextObj();

                //look for if the user has defined a custom annotation if they have we deal with that instead of making a new one
                for (int j = 0; j < currentInstance.annotations.Count; j++)
                {
                    if ((currentInstance.annotations[j].m_point.X == pointsList[i].X) &&
                        (currentInstance.annotations[j].m_point.Y == pointsList[i].Y))
                    {
                        usingCustomAnnotation = true;
                        Annotation customAnnotation = currentInstance.annotations[j];
                        PointPair pt = pointsList[i];

                        // Create a text label from the Y data value
                        text = new TextObj(customAnnotation.m_text, pt.X, pt.Y + offset,
                            CoordType.AxisXYScale, AlignH.Left, AlignV.Center);

                        // Store the point into the text object's tag
                        text.Tag = (object)pt;

                        if (customAnnotation.m_showHideAuto > 0)
                        {
                            // Always show this annotation
                            text.IsVisible = true;
                        }
                        else if (customAnnotation.m_showHideAuto < 0)
                        {
                            // Always hide this annotation
                            text.IsVisible = false;
                        }
                        else if (pt.Y <= minIntensityToDisplay)
                        {
                            // Auto Determine if we are going to show the annotation for this point
                            text.IsVisible = false;
                        }

                        break;
                    }
                }

                if (!usingCustomAnnotation)
                {
                    // Get the pointpair
                    PointPair pt = pointsList[i];

                    // Create a text label from the Y data value
                    string tagText;
                    if (pt.Tag != null)
                    {
                        tagText = pt.Tag as string;
                    }
                    else
                    {
                        tagText = string.Empty;
                    }

                    text = new TextObj(tagText, pt.X, pt.Y + offset,
                        CoordType.AxisXYScale, AlignH.Left, AlignV.Center);

                    // Store the point into the text object's tag
                    text.Tag = (object)pt;

                    // Determine if we are going to show the annotation for this point
                    if (pt.Y <= minIntensityToDisplay)
                    {
                        text.IsVisible = false;
                    }
                }

                text.IsClippedToChartRect = true; //set true because we want the annotations to hide when they go off the borders of the graph
                text.FontSpec.Size = m_options.annotationTextSize;
                text.FontSpec.FontColor = m_options.annotationColor;
                text.ZOrder = ZOrder.C_BehindChartBorder;

                // Hide the border and the fill
                text.FontSpec.Border.IsVisible = false;
                text.FontSpec.Fill.IsVisible = false;

                // Rotate the text to 90 degrees
                text.FontSpec.Angle = 90;

                myPane.GraphObjList.Add(text);
            }
        }

        /// <summary>
        /// Fires just afer the form is done zooming.  Reevaluates the annotations for the new scale
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="oldState"></param>
        /// <param name="newState"></param>
        void MyZedGraph_ZoomEvent(ZedGraphControl sender, ZoomState oldState, ZoomState newState)
        {
            if (m_manager.DataLoaded == true)
            {
                ReevaluateAnnotations();
            }
            //m_arrowShowing = false;
        }

        /// <summary>
        /// Based on how the user zoomed in the plot, we will need to reevaluate how to hide/show annoatations.
        /// </summary>
        public void ReevaluateAnnotations()
        {
            foreach (GraphPane pane in MasterPane.PaneList)
            {
                double minY = pane.YAxis.Scale.Min;
                if (m_options.zoomHorizontal == false) //For box zoom (non horizontal zoom) change the YAxis to 0 to keep the X axis
                {
                    pane.YAxis.Scale.Min = 0;
                    minY = 0;
                }
                double minX = pane.XAxis.Scale.Min;
                double maxX = pane.XAxis.Scale.Max;
                
                double maxY = pane.YAxis.Scale.Max;
                try
                {
                    PointPairList visibleMatchedPoints = GetVisiblePoints(pane.CurveList[matchedCurveName].Points, pane.CurveList[unmatchedCurveName].Points, minX, maxX, minY, maxY);
                    double minIntensityToDisplay = FindMinIntensityToDisplay(visibleMatchedPoints);
                    foreach (TextObj text in pane.GraphObjList)
                {
                    PointPair pt = (PointPair)text.Tag;
                    Annotation customAnnotation = new Annotation();

                    foreach (Annotation annotation in m_manager.GetCurrentInstance().annotations)
                    {
                        if ((annotation.m_point.X == pt.X) &&
                         (annotation.m_point.Y == pt.Y))
                        {
                            customAnnotation = annotation;
                            break;
                        }
                    }

                    if (customAnnotation.m_showHideAuto > 0)
                    {
                        // Always show this annotation
                        text.IsVisible = true;
                    }
                    else if (customAnnotation.m_showHideAuto < 0)
                    {
                        // Always hide this annotation
                        text.IsVisible = false;
                    }
                    else if (pt.Y <= minIntensityToDisplay)
                    {
                        // Auto Determine if we are going to show the annotation for this point
                        text.IsVisible = false;
                    }
                    else
                    {
                        text.IsVisible = true;
                    }
                }
                }
                catch
                {
                    // Do nothing upon catch
                }
            }
        }

        /// <summary>
        /// Returns a list of the points that are visible in the current window
        /// </summary>
        private PointPairList GetVisiblePoints(IPointList unmatchedPoints, IPointList matchedPoints, double minX, double maxX, double minY, double maxY)
        {
            PointPairList visiblePointsList = GetVisiblePoints(matchedPoints, minX, maxX, minY, maxY);
            visiblePointsList.AddRange(GetVisiblePoints(unmatchedPoints, minX, maxX, minY, maxY));

            return visiblePointsList;
        }

        /// <summary>
        /// Returns a list of the points that are visible in the given points list
        /// </summary>
        private PointPairList GetVisiblePoints(IPointList points, double minX, double maxX, double minY, double maxY)
        {
            PointPairList visibleList = new PointPairList();

            if (points != null)
            {
                for(int i = 0; i < points.Count; i++)
                {
                    PointPair point = points[i];

                    if ((point.X < maxX) && (point.X > minX) && (point.Y < maxY) && (point.Y > minY))
                    {
                        visibleList.Add(point);
                    }
                }
            }

            return visibleList;
        }

        /// <summary>
        /// From a list of matched points, finds the lowest value of relative intensity that will satisfy the top percentage requirement
        /// 
        /// Example, if our set was { 1, 5, 7, 3} and we wanted the top 50% of values, this method would return 5.
        /// </summary>
        /// <param name="pointsList"></param>
        /// <returns></returns>
        private double FindMinIntensityToDisplay(IPointList pointsList)
        {
            double minIntensity = 0.0;

            int numAnnotationsToHide = Convert.ToInt32(pointsList.Count * ((double)m_options.annotationPercent / 100.0));
            List<double> values = new List<double>();

            for (int i = 0; i < pointsList.Count; i++)
            {
                values.Add(pointsList[i].Y);
            }
            values.Sort();

            int selectIndex = values.Count - numAnnotationsToHide - 1;
            if (selectIndex >= 0)
            {
                minIntensity = values[selectIndex];
            }

            return minIntensity;
        }
        #endregion

        #region Plotting

        private const float m_standardBaseDemension = 7.0F;

        /// <summary>
        /// Creates a plot and places it into zedgraph
        /// </summary>
        public void PlotGraph(string peptide, string scanNumber, List<Element> unmatchedPointsList, List<Element> matchedPointsList)
        {
            PointPairList unmatchedPoints = MakePointPairList(unmatchedPointsList);
            PointPairList matchedPoints = MakePointPairList(matchedPointsList);

            PlotGraph(peptide, scanNumber, unmatchedPoints, matchedPoints);
        }

        /// <summary>
        /// Creates a new plot in the zedGraph Control
        /// </summary>
        /// <param name="peptide"></param>
        /// <param name="scanNumber"></param>
        /// <param name="unmatchedPointsList"></param>
        /// <param name="matchedPointsList"></param>
        public void PlotGraph(string peptide, string scanNumber, PointPairList unmatchedPointsList, PointPairList matchedPointsList)
        {
            //save the data
            m_currentPeptide = peptide;
            m_currentScanNumber = scanNumber;
            m_unmatchedPoints = unmatchedPointsList;
            m_matchedPoints = matchedPointsList;

            //clear the masterPane
            ZedGraph.MasterPane master = this.MasterPane;
            master.GraphObjList.Clear();
            master.PaneList.Clear();

            // split the points into groups
            List<PointPairList> unmatchedPointsSection;
            List<PointPairList> matchedPointsSection;
            
            // Divides the points into sections, this is used when we create more than one plot
            DividePointsIntoSections(m_options.numberOfPlots, m_matchedPoints, m_unmatchedPoints, out matchedPointsSection, out unmatchedPointsSection);
            
            // Show the masterpane title
            master.Title.IsVisible = true;
            master.Title.Text = "Peptide: " + peptide + " Scan: " + scanNumber;

            // Leave a margin around the masterpane, but only a small gap between panes
            master.Margin.All = 10;
            master.InnerPaneGap = 5;

            for (int j = 0; j < m_options.numberOfPlots; j++)
            {
                // Create a new graph -- dimensions to be set later by MasterPane Layout
                GraphPane myPaneT = new GraphPane(new Rectangle(10, 10, 10, 10),
                   "",
                   "m/z",
                   "Relative Intensity");

                // Set the BaseDimension, so fonts are scale a little bigger
                myPaneT.BaseDimension = m_standardBaseDemension / m_options.numberOfPlots;

                // Hide the XAxis scale and title
                myPaneT.XAxis.Title.IsVisible = false;
                myPaneT.XAxis.Scale.IsVisible = false;
                // Hide the legend, border, and GraphPane title
                myPaneT.Legend.IsVisible = false;
                myPaneT.Border.IsVisible = false;
                myPaneT.Title.IsVisible = false;

                // Restrict the scale to go right up to the last data point
                double matchedMax = 0;
                double unmatchedMax = 0;
                double matchedMin = double.MaxValue;
                double unmatchedMin = double.MaxValue;

                if (matchedPointsSection[j].Count > 0)
                {
                    matchedMax = matchedPointsSection[j][matchedPointsSection[j].Count - 1].X;
                    matchedMin = matchedPointsSection[j][0].X;
                }
                if (unmatchedPointsSection[j].Count > 0)
                {
                    unmatchedMax = unmatchedPointsSection[j][unmatchedPointsSection[j].Count - 1].X;
                    unmatchedMin = unmatchedPointsSection[j][0].X;
                }

                myPaneT.XAxis.Scale.Max = (matchedMax > unmatchedMax) ? matchedMax : unmatchedMax;
                myPaneT.XAxis.Scale.Min = (matchedMin < unmatchedMin) ? matchedMin : unmatchedMin;

                // Remove all margins
                myPaneT.Margin.All = 0;
                // Except, leave some top margin on the first GraphPane
                if (j == 0)
                {
                    myPaneT.XAxis.Scale.Min = myPaneT.XAxis.Scale.Min - 100;
                    myPaneT.Margin.Top = 20;
                }
                // And some bottom margin on the last GraphPane
                // Also, show the X title and scale on the last GraphPane only
                if (j == m_options.numberOfPlots - 1)
                {
                    myPaneT.XAxis.Scale.Max = myPaneT.XAxis.Scale.Max + 100;
                    myPaneT.XAxis.Title.IsVisible = true;
                    myPaneT.Legend.IsVisible = m_options.showLegend;
                    myPaneT.Legend.Position = LegendPos.BottomCenter;
                }
                myPaneT.XAxis.Scale.IsVisible = true;
                //myPaneT.Margin.Bottom = 10;
                if (j > 0)
                {
                    myPaneT.YAxis.Scale.IsSkipLastLabel = true;
                }
                // This sets the minimum amount of space for the left and right side, respectively
                // The reason for this is so that the ChartRect's all end up being the same size.
                myPaneT.YAxis.MinSpace = 80;
                myPaneT.Y2Axis.MinSpace = 20;


                // generate the lines
                // Keep the matched points in front by drawing them first.
                OHLCBarItem matchedCurve = myPaneT.AddOHLCBar(matchedCurveName, matchedPointsSection[j], m_options.matchedColor);
                matchedCurve.Bar.Width = 2;
                AddAnnotations(matchedCurve.Points, myPaneT);
                if (!m_options.hideUnmatched)
                {
                    OHLCBarItem unmatchedCurve = myPaneT.AddOHLCBar(unmatchedCurveName, unmatchedPointsSection[j], m_options.unmatchedColor);
                    AddAnnotations(unmatchedCurve.Points, myPaneT);
                }

                // Add the GraphPane to the MasterPane.PaneList
                master.Add(myPaneT);
            }

            //Tell ZedGraph to refigure the axes since the data has changed
            using (Graphics g = this.CreateGraphics())
            {
                // Align the GraphPanes vertically
                if (m_options.numberOfPlots >= 4)
                {
                    master.SetLayout(g, PaneLayout.SquareColPreferred);
                }
                else
                {
                    master.SetLayout(g, PaneLayout.SingleColumn);
                }
                master.AxisChange(g);
                this.PerformAutoScale();
            }
        }

        /// <summary>
        /// Handles reassigning all of the options in case they have been changed
        /// </summary>
        public void UpdateOptions()
        {
            if (m_unmatchedPoints != null && m_matchedPoints != null && m_unmatchedPoints.Count != 0 && m_matchedPoints.Count != 0)
            {
                SuspendLayout();

                if (m_options.replot)
                {
                    PlotGraph(this.m_currentPeptide, this.m_currentScanNumber, this.m_unmatchedPoints, this.m_matchedPoints);
                    m_options.replot = false;
                }
                else
                {
                    foreach (GraphPane myPane in this.MasterPane.PaneList)
                    {
                        //PointPairList oldUnmatchedPoints = (PointPairList)myPane.CurveList["Unmatched Peaks"].Points;
                        //PointPairList oldMatchedPoints = (PointPairList)myPane.CurveList["Matched Peaks"].Points;

                        myPane.CurveList.Clear();
                        myPane.GraphObjList.Clear();

                        if (!m_options.hideUnmatched)
                        {
                            OHLCBarItem unmatchedCurve = myPane.AddOHLCBar("Unmatched Peaks", m_unmatchedPoints, m_options.unmatchedColor);
                            AddAnnotations(unmatchedCurve.Points, myPane);
                        }
                        OHLCBarItem matchedCurve = myPane.AddOHLCBar("Matched Peaks", m_matchedPoints, m_options.matchedColor);
                        AddAnnotations(matchedCurve.Points, myPane);
                    }
                }

                ResumeLayout();
            }
        }

        private void DividePointsIntoSections(int numberSections, PointPairList originalMatched, PointPairList originalUnmatched, out List<PointPairList> matchedSections, out List<PointPairList> unmatchedSections)
        {
            //for now just to see how this looks, we are going to just duplicate the matched + unmatched lists into the sections
            unmatchedSections = new List<PointPairList>();
            matchedSections = new List<PointPairList>();
            
            //since this will be very common, we will make a quick getaway with it
            if (numberSections <= 1)
            {
                unmatchedSections.Add(originalUnmatched);
                matchedSections.Add(originalMatched);
                return;
            }

            int totNumOfPoints = originalMatched.Count + originalUnmatched.Count;
            int pointsPerSection = totNumOfPoints / numberSections;
            int unmatchedIndex = 0, matchedIndex = 0;

            for (int i = 0; i < numberSections; i++)
            {
                PointPairList tempUnmatched = new PointPairList();
                PointPairList tempMatched = new PointPairList();

                //here goes hoping that the lists are sorted...
                for (int j = 0; j < pointsPerSection; j++)
                {
                    double nextMatched = (matchedIndex < originalMatched.Count) ? originalMatched[matchedIndex].X : originalUnmatched[originalUnmatched.Count - 1].X + 1;
                    double nextUnmatched = (unmatchedIndex < originalUnmatched.Count) ? originalUnmatched[unmatchedIndex].X : originalMatched[originalMatched.Count - 1].X + 1;

                    if (nextUnmatched < nextMatched)
                    {
                        tempUnmatched.Add(originalUnmatched[unmatchedIndex]);
                        unmatchedIndex++;
                    }
                    else
                    {
                        tempMatched.Add(originalMatched[matchedIndex]);
                        matchedIndex++;
                    }
                }

                unmatchedSections.Add(tempUnmatched);
                matchedSections.Add(tempMatched);
            }

            //add points to the last section that may have been rounded off
            while (unmatchedIndex < originalUnmatched.Count)
            {
                unmatchedSections[numberSections - 1].Add(originalUnmatched[unmatchedIndex]);
                unmatchedIndex++;
            }
            while (matchedIndex < originalMatched.Count)
            {
                matchedSections[numberSections - 1].Add(originalMatched[matchedIndex]);
                matchedIndex++;
            }
        }

        /// <summary>
        /// Converts a List of Point to the PointPairList that zedGraphUses
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        PointPairList MakePointPairList(List<Element> points)
        {
            PointPairList newList = new PointPairList();

            foreach (Element point in points)
            {
                if (string.IsNullOrEmpty(point.Annotation))
                {
                    newList.Add(point.Mz, point.Intensity);
                }
                else
                {
                    newList.Add(point.Mz, point.Intensity, point.Annotation);
                }
            }

            return newList;
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Sets the drawArrow flag to true so that the plot knows to draw the arrow, also sets the point the arrow is drawn at
        /// </summary>
        /// <param name="drawPoint">the graph coordinates of where to draw the arrow</param>
        public void PaintArrow(PointF graphPoint)
        {
           
            m_arrowShowing = true;
            m_arrowPoint.X = graphPoint.X;
            m_arrowPoint.Y = graphPoint.Y;
        }

        /// <summary>
        /// Handles drawing a vertical arrow pointing up on the screen
        /// </summary>
        /// <param name="arrowPoint">the location of the tip of the arrow</param>
        public void DrawArrow(PointF arrowPoint, PaintEventArgs e)
        {
            const int penThickness = 7;
            bool Draw = true;

            if (arrowPoint.X > GraphPane.XAxis.Scale.Max || arrowPoint.X < GraphPane.XAxis.Scale.Min ||
                arrowPoint.Y > GraphPane.YAxis.Scale.Max || arrowPoint.Y < GraphPane.YAxis.Scale.Min)
            {
                Draw = false;
            }

            if (Draw)
            {
                Graphics g = e.Graphics;
                PointF drawPoint = GraphPane.GeneralTransform(arrowPoint, CoordType.AxisXYScale);

                g.SmoothingMode = SmoothingMode.AntiAlias;

                Pen p = new Pen(Color.Black, penThickness);
                p.StartCap = LineCap.Square;
                p.EndCap = LineCap.ArrowAnchor;

                //These are the coordinates on the screen of where the arrow will start and End
                Point pointStart = new Point((int)(drawPoint.X + .5), (int)(drawPoint.Y + 20.5));
                Point pointEnd = new Point((int)(drawPoint.X + .5), (int)(drawPoint.Y + .5));

                g.DrawLine(p, pointStart, pointEnd);

                g.DrawString(arrowPoint.X.ToString("0.0"), new Font(FontFamily.GenericSerif, 8), Brushes.Black, new PointF(pointStart.X - penThickness, pointStart.Y + 3));
                //p.Dispose();
            }
        }

        /// <summary>
        /// zedgraph paint override to paint what we want on top of the form
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (m_snapShowing && m_options.showSnappingCursor)
            {
                DrawSnapCursor(m_snapBoxPosition, e);
            }
            if (m_arrowShowing)
            {
                DrawArrow(m_arrowPoint, e);
            }
        }
        #endregion

        #region Right Click
        
        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (!m_options.rightClickUnzoom)
            {
                base.OnMouseClick(e);
            }
            else
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    if (m_manager.DataLoaded == true)
                    {
                        HandleZoomOut();
                    }
                }
                else
                {
                    base.OnMouseClick(e);
                }
            }
        }

        #endregion

        #region Form Events

        /// <summary>
        /// Event that fires when the control is resized
        /// </summary>
        void MyZedGraph_Resize(object sender, EventArgs e)
        {
            //m_arrowShowing = false;
        }

        /// <summary>
        /// event that fires when the mouse enters the control
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            m_arrowShowing = false;
        }

        /// <summary>
        /// Handles zooming out the plot when the user hits the zoom out button while the mouse is over the plot
        /// </summary>
        public void HandleZoomOut()
        {

            Point cursorPos = Cursor.Position;
            PointF cursorPosF = new PointF(cursorPos.X, cursorPos.Y);
            GraphPane closestPane;

            closestPane = FindNearestPane(cursorPosF);
            if (closestPane != null)
            {
                if (closestPane.ZoomStack.IsEmpty)
                {
                    //for some reason, the stack can be empty when it should have a value... so we will just replot
                    PlotGraph(this.m_currentPeptide, this.m_currentScanNumber, this.m_unmatchedPoints, this.m_matchedPoints);
                }
                else
                {
                    ZoomOut(closestPane);
                }
                UpdateSnappingCursor(cursorPosF);
            }
        }

        /// <summary>
        /// Locates the nearest pane to the mouse position
        /// </summary>
        /// <param name="mousePt"></param>
        /// <returns></returns>
        GraphPane FindNearestPane(PointF mousePt)
        {
            double closestDistance = double.MaxValue;
            GraphPane closest = null;
            
            foreach (GraphPane pane in MasterPane.PaneList)
            {
                PointPair paneCenter = new PointPair(pane.Rect.Location.X + .5 * pane.Rect.Width, pane.Rect.Location.Y + .5 * pane.Rect.Height);
                double paneDistance = this.CalculateDistance(paneCenter, new PointPair(mousePt));
                if (paneDistance < closestDistance)
                {
                    closest = pane;
                }
            }

            return closest;
        }

        #endregion
    }
}
