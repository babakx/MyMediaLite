using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyMediaLite.Data
{
    public class FeatureSet : IEnumerable<Feature>
    {
        IList<Feature> Features = new List<Feature>();

        public void Add(Feature feature)
        {
            Features.Add(feature);
        }

        public void Add(int feature_id, float feature_value)
        {
            Features.Add(new Feature() { Key = feature_id, Value = feature_value });
        }

        public IEnumerator<Feature> GetEnumerator()
        {
            return Features.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Features.GetEnumerator();
        }
    }
}
