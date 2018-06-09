using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;


namespace Zuru
{
	public class Table :
		MonoBehaviour
	{
		[Serializable]
		struct Tabletop
		{
			[Tooltip("Dimension")]
			public Vector3 dimension;

			[Tooltip("Height")]
			public float height;

			[Tooltip("Stretching handle PF")]
			public GameObject stretchingHandlePF;

			[Tooltip("Min dimension (y is ignored)")]
			public Vector3 minDimension;
		}

		[Serializable]
		struct Leg
		{
			[Tooltip("PF")]
			public GameObject PF;

			[Tooltip("Distance from corner (y is ignored)")]
			public Vector3 distance;
		}

		[Serializable]
		struct Chair
		{
			[Tooltip("PF")]
			public GameObject PF;

			[Tooltip("Min space that there has to be between chair and any other element (another chair or table leg)")]
			public float padding;
		}


		[SerializeField]
		Tabletop m_tabletop = new Tabletop();

		[SerializeField]
		Leg m_leg = new Leg();

		[SerializeField]
		Chair m_chair = new Chair();


		// Chairs
		List<GameObject> m_chairs = new List<GameObject>();

		// Currently active tabletop stretching handle
		GameObject m_handleActive = null;

		// Tabletop stretching handles
		GameObject[] m_handles;

		// Whether tabletop is in stretching state
		bool m_isStretching = false;

		// Legs
		GameObject[] m_legs;

		// Tabletop mesh and vertices
		Mesh m_mesh;
		Vector3[] m_meshVertices;

		// Mouse position at previous frame
		Vector3 m_mousePosition = Vector3.one * float.MaxValue;

		// Imaginary plane on which stretching happens
		Plane plane;


		void Awake()
		{
			/* Check that serialized fields have sane values */
			Assert.IsTrue(m_tabletop.dimension.x > m_tabletop.minDimension.x && m_tabletop.dimension.y > 0.0f &&
					m_tabletop.dimension.z > m_tabletop.minDimension.z);

			Assert.IsTrue(m_tabletop.height > 0.0f);
			Assert.IsNotNull(m_tabletop.stretchingHandlePF);

			foreach (var i in new int[] { 0, 2 })
			{
				Assert.IsTrue(m_tabletop.minDimension[i] > 0.0f);
			}

			Assert.IsNotNull(m_leg.PF);

			foreach (var i in new int[] { 0, 2 })
			{
				Assert.IsTrue(m_leg.distance[i] >= 0.0f);
			}

			Assert.IsNotNull(m_chair.PF);
			Assert.IsTrue(m_chair.padding > 0.0f);

			/* Create tabletop mesh (top view)
			 *   NW -------- NE
			 *    |          |
			 *    |          |
			 *   SW -------- SE */
			m_meshVertices = new Vector3[24];

			// Northwest vertices
			m_meshVertices[0].x = -m_tabletop.dimension.x * 0.5f;
			m_meshVertices[0].y = m_tabletop.height;
			m_meshVertices[0].z = m_tabletop.dimension.z * 0.5f;

			m_meshVertices[3] = m_meshVertices[0];
			m_meshVertices[3].y += m_tabletop.dimension.y;

			m_meshVertices[1] = m_meshVertices[2] = m_meshVertices[0];
			m_meshVertices[4] = m_meshVertices[5] = m_meshVertices[3];

			// Northeast vertices
			m_meshVertices[6].x = m_tabletop.dimension.x * 0.5f;
			m_meshVertices[6].y = m_tabletop.height;
			m_meshVertices[6].z = m_tabletop.dimension.z * 0.5f;

			m_meshVertices[9] = m_meshVertices[6];
			m_meshVertices[9].y += m_tabletop.dimension.y;

			m_meshVertices[7] = m_meshVertices[8] = m_meshVertices[6];
			m_meshVertices[10] = m_meshVertices[11] = m_meshVertices[9];

			// Southeast vertices
			m_meshVertices[12].x = m_tabletop.dimension.x * 0.5f;
			m_meshVertices[12].y = m_tabletop.height;
			m_meshVertices[12].z = -m_tabletop.dimension.z * 0.5f;

			m_meshVertices[15] = m_meshVertices[12];
			m_meshVertices[15].y += m_tabletop.dimension.y;

			m_meshVertices[13] = m_meshVertices[14] = m_meshVertices[12];
			m_meshVertices[16] = m_meshVertices[17] = m_meshVertices[15];

			// Southwest vertices
			m_meshVertices[18].x = -m_tabletop.dimension.x * 0.5f;
			m_meshVertices[18].y = m_tabletop.height;
			m_meshVertices[18].z = -m_tabletop.dimension.z * 0.5f;

			m_meshVertices[21] = m_meshVertices[18];
			m_meshVertices[21].y += m_tabletop.dimension.y;

			m_meshVertices[19] = m_meshVertices[20] = m_meshVertices[18];
			m_meshVertices[22] = m_meshVertices[23] = m_meshVertices[21];

			m_mesh = new Mesh
			{
				vertices = m_meshVertices,

				triangles = new int[]
				{
					// Left face
					3, 21, 18,
					18, 0, 3,

					// Front face
					22, 15, 12,
					12, 19, 22,

					// Top face
					4, 9, 16,
					16, 23, 4,

					// Bottom face
					6, 1, 20,
					20, 13, 6,

					// Right face
					17, 10, 7,
					7, 14, 17,

					// Back face
					11, 5, 2,
					2, 8, 11
				}
			};

			m_mesh.RecalculateNormals();

			GetComponent<MeshFilter>().mesh = m_mesh;

			/* Instantiate stretching handles and place them at tabletop corners */
			m_handles = new GameObject[4];

			for (var i = 0; i < 4; ++i)
			{
				m_handles[i] = Instantiate(m_tabletop.stretchingHandlePF, transform);
				m_handles[i].transform.localScale = Vector3.one * m_tabletop.dimension.y * 2.0f;
			}

			RepositionHandles();

			/* Instantiate legs and place them in position */
			m_legs = new GameObject[4];

			for (var i = 0; i < 4; ++i)
			{
				m_legs[i] = Instantiate(m_leg.PF, transform);
				m_legs[i].transform.localScale = new Vector3(1.0f, m_tabletop.height, 1.0f);
			}

			RepositionLegs();

			/* Place plane at tabletop barycenter and parallel to XZ */
			plane = new Plane(Vector3.up, -(m_tabletop.height + m_tabletop.dimension.y * 0.5f));

			/* Place chairs in position */
			PlaceChairs();
		}


		void Update()
		{
			if (m_mousePosition != Input.mousePosition)
			{
				m_mousePosition = Input.mousePosition;

				if (m_isStretching)
				{
					/* Stretch tabletop and update its elements (mesh, handles and legs) and chairs when handle is dragged with left mouse button */
					var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
					float distance;

					if (plane.Raycast(ray, out distance))
					{
						StretchActiveCorner(transform.InverseTransformPoint(ray.GetPoint(distance)));
						RepositionAdjacentCorners();

						m_mesh.vertices = m_meshVertices;

						m_mesh.RecalculateBounds();
						m_mesh.RecalculateNormals();

						RepositionHandles();
						RepositionLegs();

						PlaceChairs();
					}
				}
				else
				{
					/* Show tabletop stretching handle when it's under mouse cursor or hide it otherwise */
					var layerMask = 1 << LayerMask.NameToLayer("TabletopHandle");

					RaycastHit hit;
					Physics.Raycast(Camera.main.ScreenPointToRay(m_mousePosition), out hit, Camera.main.farClipPlane, layerMask);

					if (hit.transform && !m_handleActive)
					{
						m_handleActive = hit.transform.gameObject;
						m_handleActive.GetComponent<Renderer>().enabled = true;
					}
					else if (!hit.transform && m_handleActive)
					{
						m_handleActive.GetComponent<Renderer>().enabled = false;
						m_handleActive = null;
					}
				}
			}

			if (m_handleActive)
			{
				/* Enter in stretching state when left mouse button is pressed and exit when it's released */
				if (Input.GetMouseButtonDown(0))
				{
					m_isStretching = true;
				}
				else if (Input.GetMouseButtonUp(0))
				{
					m_isStretching = false;
				}
			}
		}


		// Instantiate and destroy chairs as needed and place them in position
		void PlaceChairs()
		{
			/* Instantiate chairs if missing or destroy those in excess */
			var spaceH = m_legs[1].transform.localPosition.x - m_legs[0].transform.localPosition.x - m_leg.PF.GetComponentInChildren<Renderer>().bounds.size.x -
					m_chair.padding;

			var spaceV = m_legs[1].transform.localPosition.z - m_legs[2].transform.localPosition.z - m_leg.PF.GetComponentInChildren<Renderer>().bounds.size.z -
					m_chair.padding;

			var chairSize = m_chair.PF.GetComponentInChildren<Renderer>().bounds.size.x + m_chair.padding;

			var chairCountH = Mathf.FloorToInt(spaceH / chairSize);
			var chairCountV = Mathf.FloorToInt(spaceV / chairSize);
			var chairCount = (chairCountH + chairCountV) * 2;

			if (m_chairs.Count < chairCount)
			{
				for (var i = m_chairs.Count; i < chairCount; ++i)
				{
					m_chairs.Add(Instantiate(m_chair.PF, transform));
				}
			}
			else if (m_chairs.Count > chairCount)
			{
				for (var i = m_chairs.Count; i > chairCount; --i)
				{
					Destroy(m_chairs[i - 1]);
				}

				m_chairs.RemoveRange(chairCount, m_chairs.Count - chairCount);
			}

			/* Reposition chairs */
			var chairOffset = 0;

			var orientation = Quaternion.Euler(0.0f, 180.0f, 0.0f);

			for (var i = 0; i < 4; ++i)
			{
				float currentSpace;
				int currentCount;

				if (i % 2 == 0)
				{
					currentSpace = spaceH;
					currentCount = chairCountH;
				}
				else
				{
					currentSpace = spaceV;
					currentCount = chairCountV;
				}

				var positionA = m_legs[i].transform.localPosition;
				var positionB = m_legs[(i + 1) % 4].transform.localPosition;

				var extents = (Vector3.Distance(positionA, positionB) / currentSpace - 1.0f) * 0.5f;

				for (var j = 0; j < currentCount; ++j)
				{
					// Remap t taking in account legs dimension
					var t = Mathf.InverseLerp(-extents, extents + 1.0f, (float)(j * 2 + 1) / (float)(currentCount * 2));

					m_chairs[chairOffset + j].transform.localPosition = new Vector3(Mathf.Lerp(positionA.x, positionB.x, t), 0.0f, Mathf.Lerp(positionA.z, positionB.z, t));
					m_chairs[chairOffset + j].transform.localRotation = orientation;
				}

				chairOffset += currentCount;

				orientation *= Quaternion.Euler(0.0f, 90.0f, 0.0f);
			}
		}


		// Reposition tabletop corners adjacent to currently active one (due to mesh change)
		void RepositionAdjacentCorners()
		{
			var index = Array.IndexOf(m_handles, m_handleActive);

			var prev = ((index - 1) % 4 + 4) % 4;
			var next = (index + 1) % 4;

			for (var i = 0; i < 6; ++i)
			{
				if (index % 2 == 0)
				{
					m_meshVertices[prev * 6 + i].x = m_meshVertices[index * 6 + i].x;
					m_meshVertices[next * 6 + i].z = m_meshVertices[index * 6 + i].z;
				}
				else
				{
					m_meshVertices[prev * 6 + i].z = m_meshVertices[index * 6 + i].z;
					m_meshVertices[next * 6 + i].x = m_meshVertices[index * 6 + i].x;
				}
			}
		}


		// Reposition stretching handles at tabletop corners (due to mesh change)
		void RepositionHandles()
		{
			var y = m_tabletop.height + m_tabletop.dimension.y * 0.5f;

			for (var i = 0; i < 4; ++i)
			{
				m_handles[i].transform.localPosition = new Vector3(m_meshVertices[i * 6].x, y, m_meshVertices[i * 6].z);
			}
		}


		// Reposition legs taking in account their distance from corners (due to mesh change)
		void RepositionLegs()
		{
			var t = new Vector3();

			foreach (var i in new int[] { 0, 2 })
			{
				t[i] = (m_leg.distance[i] + m_leg.PF.GetComponentInChildren<Renderer>().bounds.extents[i]) / m_mesh.bounds.extents[i];
			}

			for (var i = 0; i < 4; ++i)
			{
				var x = Mathf.Lerp(m_meshVertices[i * 6].x, m_mesh.bounds.center.x, t.x);
				var z = Mathf.Lerp(m_meshVertices[i * 6].z, m_mesh.bounds.center.z, t.z);

				m_legs[i].transform.localPosition = new Vector3(x, m_tabletop.height, z);
			}
		}


		// Stretch currently active corner taking in account tabletop minimum dimension
		void StretchActiveCorner(Vector3 position)
		{
			var index = Array.IndexOf(m_handles, m_handleActive);

			var corner = m_meshVertices[index * 6];
			var cornerOpposite = m_meshVertices[(index + 2) % 4 * 6];

			foreach (var i in new int[] { 0, 2 })
			{
				var isFlipped = position[i] > cornerOpposite[i] && cornerOpposite[i] > corner[i] ||
						position[i] < cornerOpposite[i] && cornerOpposite[i] < corner[i];

				if (isFlipped)
				{
					position[i] = corner[i];

					continue;
				}

				var distance = Mathf.Abs(position[i] - cornerOpposite[i]);

				if (distance < m_tabletop.minDimension[i])
				{
					var t = distance / m_tabletop.minDimension[i];

					position[i] = (position[i] - cornerOpposite[i] * (1.0f - t)) / t;
				}
			}

			for (var i = 0; i < 6; ++i)
			{
				m_meshVertices[index * 6 + i].x = position.x;
				m_meshVertices[index * 6 + i].z = position.z;
			}
		}
	}
}
