using System.IO;
using UnityEditor;
using Vaultbreakers.Combat;
namespace Vaultbreakers.Editor
{
    public static class VaultbreakersAudioReview
    {
        [MenuItem("Vaultbreakers/Review/Export original sound cues")]
        public static void Export()
        {
            Directory.CreateDirectory("Docs/Audio");
            Save("EnemyTell_Grunt",.12f,220,.7f);Save("EnemyTell_Shooter",.12f,1100,.7f);Save("EnemyTell_Bruiser",.38f,110,.7f);
            Save("VB_MeleeSwing",.16f,1400,.35f);Save("VB_MeleeImpact",.09f,180,.9f);
            Save("VB_RangedFire",.11f,900,.4f);Save("VB_RangedImpact",.07f,260,.8f);
            Save("VB_ShieldBlock",.1f,520,.7f);Save("VB_ShieldBreak",.3f,140,.5f);
            Save("VB_Dodge",.18f,280,.45f);Save("Dock9_Clear",.5f,440,.8f);
            Save("Dock9_Core",.9f,660,1.2f);Save("Dock9_Retry",.4f,165,.8f);Save("Dock9_Enter",.24f,330,.6f);
        }
        private static void Save(string name,float duration,float hz,float decay)
        {
            var data=PlaceholderAudio.Synthesize(name,duration,hz,decay);
            using(var writer=new BinaryWriter(File.Create("Docs/Audio/"+name+".wav")))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+data.Length*2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);
                writer.Write(22050);writer.Write(44100);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(data.Length*2);
                foreach(var sample in data)writer.Write((short)(sample*32767));
            }
        }
    }
}
