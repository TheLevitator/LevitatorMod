
/*
* Levitator's Space Engineers Modding Support Library
*
* Not used yet. It schedules tasks to run in the future
* and must be polled each update.
*
* Copyright Levitator 2015
*
* Reuse is free as long as you attribute the author.
*
* V1.0
*
*/

using System;
using System.Collections.Generic;

namespace Levitator.SE
{
    public class Scheduler : IDisposable
    {
        private SortedList<DateTime, Action> Actions = new SortedList<DateTime, Action>();

        public void Schedule(DateTime when, Action action){ Actions.Add(when, action); }
        public void Schedule(TimeSpan when, Action action) { Schedule(DateTime.Now + when, action); }
                       
        public void Update(){
            while (Actions.Count > 0 && Actions.Keys[0] <= DateTime.Now)
            {
                //Catch any exceptions outside Update()!
                Actions.Values[0]();                
                Actions.RemoveAt(0);
            }
        }

        public void Dispose(){ Actions.Clear(); }
    }
}
