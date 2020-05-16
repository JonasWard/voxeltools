﻿using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using StudioAvw.Voxels.Geometry;
using StudioAvw.Voxels.Param;

namespace StudioAvw.Voxels.Components.VoxelGrid
{
    /// <summary>
    /// Component that adds or removes voxels from a grid to meet a fixed amount of voxels.
    /// </summary>
    public class VoxelGridLimiter : GhVoxelComponent
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public VoxelGridLimiter()
            : base("VoxelGrid Limit Amount of Voxels", "VoxGridLimit",
                "Limit the amount of voxels in the grid",
                "Voxels", "Create")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new Param_VoxelGrid());
            pManager.AddParameter(new Param_VoxelGrid(), "Exclusion zome", "EVG", "A grid with voxels that cannot be accessed", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddIntegerParameter("Limit", "L", "Limit the amount to voxels to this amount", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {

            pManager.AddParameter(new Param_VoxelGrid());
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var vg = default(VoxelGrid3D);
            DA.GetData(0, ref vg);

            vg = (VoxelGrid3D) vg.Clone();
            //VoxelGrid vge = default(VoxelGrid);
            //DA.GetData<VoxelGrid>(1, ref vge);

            //if (vge == default(VoxelGrid))
            //{
               var vge = new VoxelGrid3D(vg.BBox, vg.VoxelSize);
            //}

            var i = 0;
            DA.GetData(2, ref i);

            if (vg.CountNonZero > i)
            {
                LimitGrid(ref vg, i);
                
            } else if (vg.CountNonZero < i)
            {
                IncreaseGrid(ref vg, vge, i);
            }
            AddRenderGrid(vg);
            DA.SetData(0, vg);
        }

        private void IncreaseGrid(ref VoxelGrid3D vg, VoxelGrid3D Exclusion, int count)
        {
            var trueCount = vg.CountNonZero;
            var roof = new VoxelGrid3D(vg.BBox, vg.VoxelSize);
            var tr = new Point3i(0, 0, 1);
            var possibleLocations = new List<int>();
            var foundRoof = false;
            for (var z = vg.SizeUVW.z - 1; z >= 0; z--)
            {
                if (foundRoof == true)
                {
                    break;
                }

                for (var y = 0; y < vg.SizeUVW.y; y++)
                {
                    for (var x = 0; x < vg.SizeUVW.x; x++)
                    {
                        var pt = new Point3i(x, y, z);
                        if (vg[pt] == true)
                        {
                            foundRoof = true;
                            possibleLocations.Add(vg.SizeUVW ^ pt);
                        }
                    }
                }
            }

            // location division
            // goal-current
            // difference: the first x voxels get extra.
            // then add x voxels to each location.

            var difference = count - trueCount;
            var height = Convert.ToInt32(Math.Floor((double) (difference / possibleLocations.Count)));
            var rest = difference % possibleLocations.Count;

            for (var i = 0; i < possibleLocations.Count; i++)
            {
                var makeheight = height;
                if (rest > 0)
                {
                    makeheight += 1;
                    rest--;
                }

                for (var j = 1; j <= makeheight; j++)
                {
                    var pt = new Point3i(0, 0, j);
                    var position = (vg.SizeUVW % possibleLocations[i]) + pt;
                    vg[position] = true;
                }
            }

            if (vg.CountNonZero != count)
            {
                // the grid is empty / reaches to the roof.
                // add from the bottom.
                IncreaseGridFromBottom(ref vg, Exclusion, count);
            }
            

        }

        /// <summary>
        /// Add voxels to the grid building up from the bottom to the top.
        /// </summary>
        /// <param name="vg">VoxelGrid</param>
        /// <param name="Exclusion">VoxelGrid with elements not working.</param>
        /// <param name="count">Amount of voxels to be added.</param>
        private void IncreaseGridFromBottom(ref VoxelGrid3D vg, VoxelGrid3D Exclusion, int count)
        {
            var trueIncCount = vg.CountNonZero;
            for (var z = 0; z < vg.SizeUVW.z; z++)
            {
                for (var y = 0; y < vg.SizeUVW.y; y++)
                {
                    for (var x = 0; x < vg.SizeUVW.x; x++)
                    {
                        var pt = new Point3i(x, y, z);
                        if (vg[pt] == false)
                        {
                            vg[pt] = true;
                            trueIncCount++;
                            if (trueIncCount == count)
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void LimitGrid(ref VoxelGrid3D vg, int i)
        {
            // limit, start with Z size.
            var trueCount = vg.CountNonZero;
            for (var z = vg.SizeUVW.z - 1; z >= 0; z--)
            {
                for (var y = 0; y < vg.SizeUVW.y; y++)
                {
                    for (var x = 0; x < vg.SizeUVW.x; x++)
                    {
                        if (vg[new Point3i(x, y, z)] == true)
                        {
                            vg[new Point3i(x, y, z)] = false;
                            trueCount--;
                        }
                        if (trueCount == i)
                        {
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon =>
            //You can add image files to your project resources and access them like this:
            // return Resources.IconForThisComponent;
            Images.VT_Statistics;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{E7A9CAF0-D295-4A4D-937D-550EF3EE3944}");
    }
}