using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PointCloudExporter
{
	public class ReaderInfos
	{
		public int cursor = 0;
		public string lineText = "";
		public bool header = true;
		public int colorDataCount = 3;
		public int index = 0;
        public int step = 0;
        public int normalDataCount = 0;
        public int levelOfDetails = 1;
	}
}
