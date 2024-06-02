using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonAPI;
using CommonAPI.Phone;
using Reptile;

namespace ScoreAttack
{
    public class AppStageSelect : CustomApp
    {
        /// <summary>
        /// Don't show in home screen.
        /// </summary>
        public override bool Available => false;

        // This app just lets us teleport to any stage. Even though it's not visible in the homescreen, we still have to register it with PhoneAPI to be able to use it.
        public static void Initialize()
        {
            PhoneAPI.RegisterApp<AppStageSelect>("stage select");
        }

        public override void OnAppInit()
        {
            base.OnAppInit();
            CreateIconlessTitleBar("Stage Select");
            ScrollView = PhoneScrollView.Create(this);

            var button = CreateStageButton("Hideout", Stage.hideout);
            ScrollView.AddButton(button);

            button = CreateStageButton("Versum Hill", Stage.downhill);
            ScrollView.AddButton(button);

            button = CreateStageButton("Millennium Square", Stage.square);
            ScrollView.AddButton(button);

            button = CreateStageButton("Brink Terminal", Stage.tower);
            ScrollView.AddButton(button);

            button = CreateStageButton("Millennium Mall", Stage.Mall);
            ScrollView.AddButton(button);

            button = CreateStageButton("Mataan", Stage.osaka);
            ScrollView.AddButton(button);

            button = CreateStageButton("Pyramid Island", Stage.pyramid);
            ScrollView.AddButton(button);

            button = CreateStageButton("Police Station", Stage.Prelude);
            ScrollView.AddButton(button);

        }

        private SimplePhoneButton CreateStageButton(string label, Stage stage)
        {
            var button = PhoneUIUtility.CreateSimpleButton(label);
            button.OnConfirm += () =>
            {
                Core.Instance.BaseModule.StageManager.ExitCurrentStage(stage);
            };
            return button;
        }
    }
}
