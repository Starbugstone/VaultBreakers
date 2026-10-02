using UnityEngine;
using UnityEngine.UI;

namespace Vaultbreakers.UI
{
    /// <summary>Small original vector symbols, kept sharp at both supported HUD resolutions.</summary>
    public sealed class CombatGlyph : MaskableGraphic
    {
        public int Symbol { get; set; }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            switch (Symbol)
            {
                case 0: // Melee: blade, guard, grip.
                    Line(mesh, .22f,.18f,.74f,.78f,3);
                    Line(mesh, .36f,.42f,.64f,.67f,7);
                    Line(mesh, .23f,.47f,.49f,.24f,3);
                    break;
                case 1: // Fire: three forward pulses.
                    for(var i=0;i<3;i++) { var x=.22f+i*.23f; Line(mesh,x,.27f,x+.16f,.50f,3);Line(mesh,x+.16f,.50f,x,.73f,3); }
                    break;
                case 2: // Guard: directional shield outline.
                    Line(mesh,.22f,.78f,.78f,.78f,3);Line(mesh,.22f,.78f,.27f,.39f,3);
                    Line(mesh,.78f,.78f,.73f,.39f,3);Line(mesh,.27f,.39f,.5f,.19f,3);Line(mesh,.73f,.39f,.5f,.19f,3);
                    Line(mesh,.5f,.35f,.5f,.65f,2);
                    break;
                default: // Dodge: running streak and direction.
                    Line(mesh,.17f,.35f,.55f,.35f,3);Line(mesh,.1f,.51f,.48f,.51f,3);
                    Line(mesh,.25f,.67f,.58f,.67f,3);Line(mesh,.60f,.24f,.84f,.5f,3);Line(mesh,.84f,.5f,.60f,.76f,3);
                    break;
            }
        }
        private void Line(VertexHelper mesh,float x1,float y1,float x2,float y2,float width)
        {
            var rect=rectTransform.rect;
            var a=new Vector2(rect.x+x1*rect.width,rect.y+y1*rect.height);
            var b=new Vector2(rect.x+x2*rect.width,rect.y+y2*rect.height);
            var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width*.5f;
            var start=mesh.currentVertCount;
            mesh.AddVert(a-n,color,Vector2.zero);mesh.AddVert(a+n,color,Vector2.zero);
            mesh.AddVert(b+n,color,Vector2.zero);mesh.AddVert(b-n,color,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
        }
    }
}
