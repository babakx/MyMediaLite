using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyMediaLite.DataType;

namespace MyMediaLite.Data
{
    public class FeatureBasedPositiveFeedback<T> : PosOnlyFeedback<T> where T : IBooleanMatrix, new()
    {
        public static MultiKeyDictionary<int, int, FeatureSet> Features { get; private set; }
        public static int MaxFeatureId { get; set; }

        public FeatureBasedPositiveFeedback()
            : base()
        {
            if (Features == null)
                Features = new MultiKeyDictionary<int, int, FeatureSet>();
        }
    }
}
