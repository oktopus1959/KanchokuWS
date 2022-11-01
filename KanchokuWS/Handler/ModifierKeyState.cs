namespace KanchokuWS.Handler
{
    class ModifierKeyState
    {
        /// <summary>事前キー操作はキーの解放か</summary>
        public bool OperationUp;

        /// <summary>左修飾キーが押下状態か</summary>
        public bool LeftKeyDown;

        /// <summary>右修飾キーが押下状態か</summary>
        public bool RightKeyDown;

        /// <summary>左右どちらかの修飾キーが押下状態か</summary>
        public bool AnyKeyDown => LeftKeyDown || RightKeyDown;

        public ModifierKeyState()
        {
            OperationUp = false;
            LeftKeyDown = false;
            RightKeyDown = false;
        }

        public ModifierKeyState(bool opeUp, bool leftDown, bool rightDown)
        {
            OperationUp = opeUp;
            LeftKeyDown = leftDown;
            RightKeyDown = rightDown;
        }
    }
}
