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

        public ModifierKeyState(bool opeUp = false, bool leftDown = false, bool rightDown = false)
        {
            OperationUp = opeUp;
            LeftKeyDown = leftDown;
            RightKeyDown = rightDown;
        }
    }
}
