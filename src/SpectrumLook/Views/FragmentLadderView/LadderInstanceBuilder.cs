﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpectrumLook.Views.FragmentLadderView
{
    /// <summary>
    /// This object is used to generate a Ladder instances based on the given theory list and peptide sequence that the theory list is generated from.
    /// By Patrick Tobin
    /// </summary>
    public class LadderInstanceBuilder
    {
        #region CONSTRUCTOR

        public LadderInstanceBuilder()
        {
        }

        #endregion

        #region FUNCTIONS

        #region PUBLIC

        /// <summary>
        /// This function generates a LadderInstance based upon the given theoryList and the given peptide.
        /// </summary>
        /// <param name="theoryList">A list of elements that have been processed through the ComparedListBuilder.</param>
        /// <param name="peptide">The Peptide sequence that the List of Elements are generated from.</param>
        /// <returns></returns>
        public LadderInstance GenerateInstance(List<SpectrumLook.Builders.Element> theoryList, string peptide, Dictionary<char, double> modificationValues)
        {
            int i = 0;
            List<string[]> tempListHolder = new List<string[]>();
            List<string> tempListColumnOptions = new List<string>();
            LadderInstance returnedLadderInstance = new LadderInstance();

            foreach (SpectrumLook.Builders.Element currentElement in theoryList)
            {
                i = 0;
                if (!(tempListColumnOptions.Contains(spliceNumberFromAnnotation(currentElement.Annotation))))
                {
                    //Need to filter out Modification values when calculating the length of the string.
                    tempListHolder.Add(new string[peptideLength(peptide, modificationValues)]);
                    tempListColumnOptions.Add(spliceNumberFromAnnotation(currentElement.Annotation));
                }
                //Find the index to add.
                while (tempListColumnOptions[i] != spliceNumberFromAnnotation(currentElement.Annotation))
                {
                    ++i;
                }
                string tempDoubleString = string.Format("{0:#.00}", currentElement.Mz);
                try
                {
                    tempListHolder[i][unformatAnnotation(currentElement.Annotation) - 1] = tempDoubleString + "|" + currentElement.Matched.ToString();
                }
                catch { }
            }

            for (i = 0; i < tempListColumnOptions.Count; ++i)
            {
                for (int j = 0; j < tempListHolder[i].Length; ++j)
                {
                    if (tempListHolder[i][j] == null)
                    {
                        tempListHolder[i][j] = "";
                    }
                }
            }

            returnedLadderInstance.mzValue = tempListHolder;
            returnedLadderInstance.mzValueHeaders = tempListColumnOptions;
            return returnedLadderInstance;
        }

        #endregion

        #region PRIVATE

        /// <summary>
        /// This function parses out a given annotation and outputs the proper ion catagory.
        /// (i.e. spliceNumberFromAnnotation("b14++") returns "b++")
        /// </summary>
        /// <param name="annotation">The annotation that is stored in the Element object.</param>
        /// <returns></returns>
        private string spliceNumberFromAnnotation(string annotation)
        {
            int i = 1;
            string outputString = annotation;
            while (char.IsNumber(outputString, i))
            {
                outputString = outputString.Remove(i, 1);
                if (outputString.Length == i)
                    return outputString;
            }
            return outputString;
        }

        /// <summary>
        /// This parses out the number inside the given annotation.
        /// (i.e. unformatAnnotation("b16++") returns 16
        /// </summary>
        /// <param name="annotation">The annotation that is stored in the Element object.</param>
        /// <returns></returns>
        private int unformatAnnotation(string annotation)
        {
            string tmpNumberString = "";
            int currentAnnotationIndex = 1;
            while (char.IsNumber(annotation, currentAnnotationIndex))
            {
                tmpNumberString += annotation[currentAnnotationIndex];
                ++currentAnnotationIndex;
                if (currentAnnotationIndex == annotation.Length)
                    break;
            }

            int.TryParse(tmpNumberString, out currentAnnotationIndex);

            return currentAnnotationIndex;
        }

        private int peptideLength(string peptide, Dictionary<char, double> modificationList)
        {
            int count = 0;
            foreach (char c in peptide)
            {
                if (modificationList.ContainsKey(c))
                {
                    count++;
                }
            }
            return peptide.Length - count;
        }

        #endregion

        #endregion
    }
}
