﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpectrumLook.Views
{
    //TODO : Need to add a location for the ComparedListBuilder.
    [Serializable]
    public class MainFormOptions : Subject
    {
        #region MEMBERS

        #region PRIVATE
        private bool m_isPlotInMainForm;
        private double m_toleranceValue;
        private double m_lowerToleranceValue;
        #endregion

        #region PUBLIC
        #endregion

        #endregion

        #region PROPERTIES

        #region PRIVATE
        #endregion

        #region PUBLIC
        public bool isPlotInMainForm
        {
            get
            {
                return m_isPlotInMainForm;
            }
            set
            {
                m_isPlotInMainForm = value;
                this.Invoke();
            }
        }

        public double toleranceValue
        {
            get
            {
                return m_toleranceValue;
            }
            set
            {
                if (value >= 0.0)
                {
                    m_toleranceValue = value;
                    this.Invoke();
                }
            }
        }

        public double lowerToleranceValue
        {
            get
            {
                return m_lowerToleranceValue;
            }
            set
            {
                if (value >= 0.0)
                {
                    m_lowerToleranceValue = value;
                    this.Invoke();
                }
            }
        }
        #endregion

        #endregion

        #region CONSTRUCTOR
        public MainFormOptions()
        {
            this.m_isPlotInMainForm = true;
            this.m_toleranceValue = 0.7;
            this.m_lowerToleranceValue = 0.0;
        }

        public MainFormOptions(MainFormOptions optionsToCopy)
        {
            m_isPlotInMainForm = optionsToCopy.isPlotInMainForm;
            m_toleranceValue = optionsToCopy.toleranceValue;
            m_lowerToleranceValue = optionsToCopy.lowerToleranceValue;
        }
        #endregion

        #region FUNCTIONS

        #region PRIVATE
        #endregion

        #region PROTECTED
        #endregion

        #region PUBLIC

        public void CopyOptions(MainFormOptions optionsToCopy)
        {
            isPlotInMainForm = optionsToCopy.isPlotInMainForm;
            toleranceValue = optionsToCopy.toleranceValue;
            lowerToleranceValue = optionsToCopy.lowerToleranceValue;
        }

        #endregion

        #endregion
    }
}
