using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Modding;
using Modding.Blocks;
using UnityEngine;

namespace NoModChecker
{
    class ModBehaviour : MonoBehaviour
    {
        private readonly int windowID = ModUtility.GetWindowId();
        Rect windowRect = new Rect(20, 300, 200, 100);

        string userInfo;

        public readonly int[] legalBlocks = new int[] {0,
        15, 1, 63, 41, 7, 5, 19, 44, 10, 49, 57, 58,
        28, 13, 2, 40, 46, 60, 50, 38, 39, 51,
        4, 9, 16, 42, 18, 22, 27, 45,
        20, 3, 17, 48, 11, 53, 61, 21, 62, 56, 47, 23, 54, 59, 31, 36,
        14, 26, 55, 25, 34, 35, 43,
        24, 32, 29, 33, 37, 30, 6};


        private readonly int[] wheelBlocks = new int[] { 2, 22, 46, 39, 17, 48 };//轮子类零件存在加速属性，可以设置为正无穷，这时简单判断slider会误判为用了无限滑条

        void OnGUI()
        {
            if (!StatMaster.levelSimulating && !StatMaster.inMenu)
            {
                this.windowRect = GUILayout.Window(this.windowID, this.windowRect, new GUI.WindowFunction(this.NoModCheckerWindow), "纯原版检测器");
            }
        }
        void NoModCheckerWindow(int id)
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("检测"))
            {
                CheckButtonClicked();
            }
            GUILayout.Label(this.userInfo);
#if DEBUG
            if (GUILayout.Button("打印零件表"))
            {
                PrintAntennaList();
            }
#endif
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        void CheckButtonClicked()
        {
            Modding.Blocks.PlayerMachine myMachine = Modding.Blocks.PlayerMachine.GetLocal();
            this.userInfo = "";
            int hasUnlegalBlock = 0;
            int hasScaledBlock = 0;
            int hasEnhancedSlider = 0;
            int hasSkin = 0;

            int numOfCores = 0;
            foreach (var block in myMachine.BuildingBlocks)
            {
                bool hasProblem = false;
                var blockInfo = Modding.Blocks.BlockInfo.From(block);
                //核心只能有一个
                if (blockInfo.Type == 0) {
                    numOfCores++;
                }
                //检查是否是mod方块或官方隐藏方块
                if (!legalBlocks.Contains(blockInfo.Type))
                {
                    hasUnlegalBlock++;
                    hasProblem = true;
                }
                //检查是否有缩放
                var scale = blockInfo.Scale;
                if (scale.x > 1.01f || scale.x < 0.99f || scale.y > 1.01f || scale.y < 0.99f || scale.z > 1.01f || scale.z < 0.99f)
                {
                    hasScaledBlock++;
                    hasProblem = true;
                }
                //检查是否有无限滑条
                foreach(var slider in block.InternalObject.Sliders)
                {
                    if (slider.DisableLimits) continue;//如果滑条没有限制，则跳过
                    if(slider.Value<slider.Min || slider.Value > slider.Max)
                    {
                        if (wheelBlocks.Contains(blockInfo.Type))//是轮子零件要特别注意一下
                        {
                            if (!slider.Key.Equals("acceleration")||slider.Value<slider.Min||(slider.Value>slider.Max&&slider.Value!=float.PositiveInfinity))
                            {
                                hasEnhancedSlider++;
                                hasProblem = true;
                                break;
                            }
                        }
                        else
                        {
                            hasEnhancedSlider++;
                            hasProblem = true;
                            break;
                        }
                    }
                }
                //检查是否有皮肤
                // to do
                if (!blockInfo.InternalObject.Skin.isDefault)
                {
                    hasSkin++;
                    hasProblem = true;
                }

                if (hasProblem)//有问题的方块我们进行特殊显示
                {
                    var renderer = block.InternalObject.MeshRenderer;
                    renderer.material = null;
                    var braceCode = block.InternalObject.gameObject.GetComponent<BraceCode>();
                    if (!(braceCode is null)) braceCode.MeshRenderer.material = null;
                }
                else
                {
                    var renderer = block.InternalObject.MeshRenderer;
                    var bb = Modding.Blocks.BlockPrefabInfo.FromId(blockInfo.Type).InternalObject.gameObject.GetComponent<BlockBehaviour>();
                    renderer.material = bb.MeshRenderer.material;
                    var braceCode = block.InternalObject.gameObject.GetComponent<BraceCode>();
                    if (!(braceCode is null))
                    {
                        braceCode.MeshRenderer.material =
                            Modding.Blocks.BlockPrefabInfo.FromId(blockInfo.Type).InternalObject.gameObject.GetComponent<BraceCode>().
                            MeshRenderer.material;
                    }
                }
            }
            if (numOfCores != 1)
            {
                this.userInfo += "存在" + numOfCores.ToString() + "个核心方块\n";
            }
            if (hasUnlegalBlock > 0)
            {
                this.userInfo += "存在" + hasUnlegalBlock.ToString() + "个非原版方块\n";
            }
            if (hasScaledBlock > 0)
            {
                this.userInfo += "存在" + hasScaledBlock.ToString() + "个缩放了的方块\n";
            }
            if (hasEnhancedSlider > 0)
            {
                this.userInfo += "存在" + hasEnhancedSlider.ToString() + "个使用了无限滑条的方块\n";
            }
            if (hasSkin > 0)
            {
                this.userInfo += "存在" + hasSkin.ToString() + "个使用了皮肤的方块\n";
            }
            if (numOfCores == 1 && hasUnlegalBlock == 0 && hasScaledBlock == 0 && hasEnhancedSlider == 0 && hasSkin == 0)
            {
                this.userInfo += "本机械为纯原版";
            }
        }
        void PrintAntennaList()
        {
            string info = "";
            foreach(int i in legalBlocks)
            {
                var prefab = Modding.Blocks.BlockPrefabInfo.FromId(i);
                var rigibody = prefab.InternalObject.gameObject.GetComponent<Rigidbody>();
                info += i.ToString() + " " + prefab.Name + " mass:" + rigibody.mass+"\n";
            }
            ModConsole.Log(info);

            /*var blockPrefabInfo = BlockPrefabInfo.FromId(0);
            StringBuilder sb = new StringBuilder("");
            printGo(blockPrefabInfo.InternalObject.gameObject, sb);
            TextWriter tw = ModIO.CreateText("treeOutput.txt");
            tw.Write(sb.ToString());
            tw.Close();*/
        }
        public void printGo(GameObject obj, StringBuilder sb, int space = 0)
        {
            string spc = "";
            for (int i = 0; i < space; i++) spc += " ";
            sb.Append(spc + "GameObject:" + obj.name + "\r\n");
            foreach (var comp in obj.GetComponents<Component>())
            {
                sb.Append(spc + "  Component:" + comp.GetType().ToString() + " " + comp.name + "\r\n");
            }
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                var childObj = obj.transform.GetChild(i).gameObject;
                printGo(childObj, sb, space + 2);
            }
        }
    }
}
