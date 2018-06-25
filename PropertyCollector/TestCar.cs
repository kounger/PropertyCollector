namespace PropertyCollector
{
    /// <summary>
    /// This is a test class.
    /// </summary>
    class TestCar
    {
        public TestCar(int id)
        {
            this.Id = id;
        }

        /// <summary>
        /// This is the summary for property Id.
        /// </summary>
        public int Id
        { get; set; }

        /// <summary>
        /// Internal Class Car.
        /// </summary>
        public class Car
        {
            public Car(string name, int serialNumber, bool automatic)
            {
                this.Name = name;
                this.SerialNumber = serialNumber;
                this.Automatic = automatic;
            }

            /// <summary>
            /// This is the summary for property Name.
            /// </summary>
            public string Name
            { get; set; }

            public int SerialNumber
            { get; set; }

            /// <summary>
            /// This is the summary for property CurrentSpeed.
            /// </summary>
            public double CurrentSpeed
            { get; set; }

            /// <summary>
            /// This is the summary for property Automatic.
            /// </summary>
            protected bool Automatic
            { get; set; }

            /// <summary>
            /// Internal Class Interior.
            /// </summary>
            public class Interior
            {

                public Interior(int numberSeats, int CupHolder)
                {
                    this.NumberSeats = numberSeats;
                    this.CupHolder = CupHolder;
                }

                /// <summary>
                /// This is the summary for property NumberSeats.
                /// </summary>
                public int NumberSeats
                { get; set; }

                /// <summary>
                /// This is the summary for property CupHolder.
                /// </summary>
                public int CupHolder
                { get; set; }

                public string CurrentDriver { get; set; } = null;
            }
        }
    }
}