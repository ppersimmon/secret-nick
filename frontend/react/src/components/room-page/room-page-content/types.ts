import type { GetParticipantsResponse, GetRoomResponse } from "@types/api";

export interface RoomPageContentProps {
  participants: GetParticipantsResponse;
  roomDetails: GetRoomResponse;
  onDrawNames: () => void;
  onUserDeleted: () => void;
  isDrawHappened: boolean;
}
