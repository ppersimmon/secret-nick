import { useState } from "react";
import { useParams } from "react-router";
import ParticipantCard from "@components/common/participant-card/ParticipantCard";
import ParticipantDetailsModal from "@components/common/modals/participant-details-modal/ParticipantDetailsModal";
import ConfirmDeleteModal from "@components/common/modals/confirm-delete-modal/ConfirmDeleteModal";
import type { Participant } from "@types/api";
import {
  MAX_PARTICIPANTS_NUMBER,
  generateParticipantLink,
} from "@utils/general";
import { type ParticipantsListProps, type PersonalInformation } from "./types";
import "./ParticipantsList.scss";
import useToaster from "@hooks/useToaster";
import { deleteUser } from "@utils/general";

const ParticipantsList = ({
  participants,
  onUserDeleted,
  isDrawHappened,
}: ParticipantsListProps) => {
  const { userCode } = useParams();
  const { showToast } = useToaster();
  const [selectedParticipant, setSelectedParticipant] =
    useState<PersonalInformation | null>(null);

  const [participantToDelete, setParticipantToDelete] =
    useState<Participant | null>(null);

  const admin = participants?.find((participant) => participant?.isAdmin);
  const restParticipants = participants?.filter(
    (participant) => !participant?.isAdmin,
  );

  const isParticipantsMoreThanTen = participants.length > 10;

  const handleInfoButtonClick = (participant: Participant) => {
    const personalInfoData: PersonalInformation = {
      firstName: participant.firstName,
      lastName: participant.lastName,
      phone: participant.phone,
      deliveryInfo: participant.deliveryInfo,
      email: participant.email,
      link: generateParticipantLink(participant.userCode),
    };
    setSelectedParticipant(personalInfoData);
  };

  const handleModalClose = () => setSelectedParticipant(null);
  const handleDeleteClick = (user: Participant) => {
    setParticipantToDelete(user);
  };

  const handleModalDeleteClose = () => {
    setParticipantToDelete(null);
  };

  const handleConfirmDelete = async () => {
    if (!participantToDelete || !admin?.userCode) return;

    try {
      await deleteUser(participantToDelete.id, admin.userCode);
      showToast("Participant successfully removed.", "success", "large");
      onUserDeleted();
      handleModalDeleteClose();
    } catch (error) {
      showToast((error as Error).message, "error", "large");
      handleModalDeleteClose();
    }
  };

  return (
    <div
      className={`participant-list ${isParticipantsMoreThanTen ? "participant-list--shift-bg-image" : ""}`}
    >
      <div
        className={`participant-list__content ${isParticipantsMoreThanTen ? "participant-list__content--extra-padding" : ""}`}
      >
        <div className="participant-list-header">
          <h3 className="participant-list-header__title">Who’s Playing?</h3>

          <span className="participant-list-counter__current">
            {participants?.length ?? 0}/
          </span>

          <span className="participant-list-counter__max">
            {MAX_PARTICIPANTS_NUMBER}
          </span>
        </div>

        <div className="participant-list__cards">
          {admin ? (
            <ParticipantCard
              key={admin?.id}
              firstName={admin?.firstName}
              lastName={admin?.lastName}
              isCurrentUser={userCode === admin?.userCode}
              isAdmin={admin?.isAdmin}
              isCurrentUserAdmin={userCode === admin?.userCode}
              adminInfo={`${admin?.phone}${admin?.email ? `\n${admin?.email}` : ""}`}
              participantLink={generateParticipantLink(admin?.userCode)}
            />
          ) : null}

          {restParticipants?.map((user) => (
            <ParticipantCard
              key={user?.id}
              firstName={user?.firstName}
              lastName={user?.lastName}
              isCurrentUser={userCode === user?.userCode}
              isCurrentUserAdmin={userCode === admin?.userCode}
              participantLink={generateParticipantLink(user?.userCode)}
              onInfoButtonClick={
                userCode === admin?.userCode && userCode !== user?.userCode
                  ? () => handleInfoButtonClick(user)
                  : undefined
              }
              onDeleteButtonClick={
                userCode === admin?.userCode &&
                userCode !== user?.userCode &&
                !isDrawHappened
                  ? () => handleDeleteClick(user)
                  : undefined
              }
            />
          ))}
        </div>

        {selectedParticipant ? (
          <ParticipantDetailsModal
            isOpen={!!selectedParticipant}
            onClose={handleModalClose}
            personalInfoData={selectedParticipant}
          />
        ) : null}

        {participantToDelete ? (
          <ConfirmDeleteModal
            isOpen={!!participantToDelete}
            onClose={handleModalDeleteClose}
            onConfirmDelete={handleConfirmDelete}
            userName={`${participantToDelete.firstName} ${participantToDelete.lastName}`}
          />
        ) : null}
      </div>
    </div>
  );
};

export default ParticipantsList;
