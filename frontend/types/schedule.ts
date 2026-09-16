export interface Schedule {
  id: string;
  staffId: string;
  staffName: string;
  workDate: string;
  startTime: string;
  endTime: string;
}

export interface CreateScheduleRequest {
  staffId: string;
  workDate: string;
  startTime: string;
  endTime: string;
}
