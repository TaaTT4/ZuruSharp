using UnityEngine;
using UnityEngine.Assertions;


namespace Zuru
{
	public class Tabletop :
		MonoBehaviour
	{
		// Tabletop top view
		//   NW -------- NE
		//    |          |
		//    |          |
		//   SW -------- SE
		enum Corner
		{
			Northwest,
			Northeast,
			Southeast,
			Southwest
		}


		[SerializeField]
		[Tooltip("Tabletop initial dimension (don't change it at runtime)")]
		Vector3 m_initialDimension;

		[SerializeField]
		[Tooltip("Tabletop initial height (don't change it at runtime)")]
		float m_initialHeight;


		// Tabletop mesh and vertices
		Mesh m_mesh;
		Vector3[] m_meshVertices;


		void Awake()
		{
			/* Check that serialized fields have sane values */
			Assert.IsTrue(m_initialDimension.x > 0.0f && m_initialDimension.y > 0.0f && m_initialDimension.z > 0.0f);
			Assert.IsTrue(m_initialHeight > 0.0f);

			/* Create tabletop mesh */
			m_meshVertices = new Vector3[24];

			// Northwest vertices
			m_meshVertices[0].x = -m_initialDimension.x * 0.5f;
			m_meshVertices[0].y = m_initialHeight;
			m_meshVertices[0].z = m_initialDimension.z * 0.5f;

			m_meshVertices[3] = m_meshVertices[0];
			m_meshVertices[3].y += m_initialDimension.y;

			m_meshVertices[1] = m_meshVertices[2] = m_meshVertices[0];
			m_meshVertices[4] = m_meshVertices[5] = m_meshVertices[3];

			// Northeast vertices
			m_meshVertices[6].x = m_initialDimension.x * 0.5f;
			m_meshVertices[6].y = m_initialHeight;
			m_meshVertices[6].z = m_initialDimension.z * 0.5f;

			m_meshVertices[9] = m_meshVertices[6];
			m_meshVertices[9].y += m_initialDimension.y;

			m_meshVertices[7] = m_meshVertices[8] = m_meshVertices[6];
			m_meshVertices[10] = m_meshVertices[11] = m_meshVertices[9];

			// Southeast vertices
			m_meshVertices[12].x = m_initialDimension.x * 0.5f;
			m_meshVertices[12].y = m_initialHeight;
			m_meshVertices[12].z = -m_initialDimension.z * 0.5f;

			m_meshVertices[15] = m_meshVertices[12];
			m_meshVertices[15].y += m_initialDimension.y;

			m_meshVertices[13] = m_meshVertices[14] = m_meshVertices[12];
			m_meshVertices[16] = m_meshVertices[17] = m_meshVertices[15];

			// Southwest vertices
			m_meshVertices[18].x = -m_initialDimension.x * 0.5f;
			m_meshVertices[18].y = m_initialHeight;
			m_meshVertices[18].z = -m_initialDimension.z * 0.5f;

			m_meshVertices[21] = m_meshVertices[18];
			m_meshVertices[21].y += m_initialDimension.y;

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
		}
	}
}
